using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;
using System.Text;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Walks supported DOM elements and materializes a StyleNode tree using the provided style factory.
/// </summary>
internal sealed class StyleTraversal
{
    private static readonly HashSet<string> SupportedTags =
        new(
            [
                HtmlCssConstants.HtmlTags.Body,
                HtmlCssConstants.HtmlTags.H1,
                HtmlCssConstants.HtmlTags.H2,
                HtmlCssConstants.HtmlTags.H3,
                HtmlCssConstants.HtmlTags.H4,
                HtmlCssConstants.HtmlTags.H5,
                HtmlCssConstants.HtmlTags.H6,
                HtmlCssConstants.HtmlTags.P,
                HtmlCssConstants.HtmlTags.Span,
                HtmlCssConstants.HtmlTags.Div,
                HtmlCssConstants.HtmlTags.Table,
                HtmlCssConstants.HtmlTags.Tbody,
                HtmlCssConstants.HtmlTags.Thead,
                HtmlCssConstants.HtmlTags.Tfoot,
                HtmlCssConstants.HtmlTags.Tr,
                HtmlCssConstants.HtmlTags.Td,
                HtmlCssConstants.HtmlTags.Th,
                HtmlCssConstants.HtmlTags.Img,
                HtmlCssConstants.HtmlTags.Hr,
                HtmlCssConstants.HtmlTags.Br,
                HtmlCssConstants.HtmlTags.Ul,
                HtmlCssConstants.HtmlTags.Ol,
                HtmlCssConstants.HtmlTags.Li,
                HtmlCssConstants.HtmlTags.Section,
                HtmlCssConstants.HtmlTags.Main,
                HtmlCssConstants.HtmlTags.Header,
                HtmlCssConstants.HtmlTags.Footer,
                HtmlCssConstants.HtmlTags.B,
                HtmlCssConstants.HtmlTags.I,
                HtmlCssConstants.HtmlTags.Strong,
                HtmlCssConstants.HtmlTags.U,
                HtmlCssConstants.HtmlTags.S
            ],
            StringComparer.OrdinalIgnoreCase);

    public StyleNode Build(IElement root, Func<IElement, ComputedStyle?, ComputedStyle> styleFactory)
    {
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        if (styleFactory is null)
        {
            throw new ArgumentNullException(nameof(styleFactory));
        }

        var state = new TraversalIdentityState();
        var rootFacts = CreateFacts(root);
        var rootIdentity = state.CreateRootIdentity(rootFacts);

        return BuildNode(root, rootFacts, rootIdentity, null, styleFactory, state);
    }

    private StyleNode BuildNode(
        IElement element,
        StyledElementFacts facts,
        StyleSourceIdentity identity,
        ComputedStyle? parent,
        Func<IElement, ComputedStyle?, ComputedStyle> styleFactory,
        TraversalIdentityState state)
    {
        var style = styleFactory(element, parent);
        var children = new List<StyleNode>();
        var content = new List<StyleContentNode>();

        var contentIndex = 0;
        var childElementIndex = 0;
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                var textIdentity = state.CreateContentIdentity(
                    identity,
                    "text",
                    contentIndex++);
                content.Add(StyleContentNode.ForText(textIdentity, child.TextContent));
                continue;
            }

            if (child is not IElement childElement)
            {
                continue;
            }

            if (!ShouldInclude(childElement))
            {
                AppendUnsupportedContent(
                    childElement,
                    identity,
                    content,
                    ref contentIndex,
                    state);
                continue;
            }

            var childFacts = CreateFacts(childElement);
            var childIdentity = state.CreateChildIdentity(
                identity,
                childFacts,
                childElementIndex++);
            var elementContentIdentity = state.CreateContentIdentity(
                identity,
                "element",
                contentIndex++);
            var childNode = BuildNode(
                childElement,
                childFacts,
                childIdentity,
                style,
                styleFactory,
                state);
            children.Add(childNode);
            content.Add(StyleContentNode.ForElement(elementContentIdentity, childNode));
        }

        return new StyleNode(identity, facts, style, children, content);
    }

    private static void AppendUnsupportedContent(
        IElement element,
        StyleSourceIdentity owner,
        ICollection<StyleContentNode> content,
        ref int contentIndex,
        TraversalIdentityState state)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                var contentIdentity = state.CreateContentIdentity(
                    owner,
                    "text",
                    contentIndex++);
                content.Add(StyleContentNode.ForText(contentIdentity, child.TextContent));
                continue;
            }

            if (child is not IElement childElement)
            {
                continue;
            }

            if (IsLineBreak(childElement))
            {
                var contentIdentity = state.CreateContentIdentity(
                    owner,
                    "line-break",
                    contentIndex++);
                content.Add(StyleContentNode.ForLineBreak(contentIdentity));
            }

            AppendUnsupportedContent(childElement, owner, content, ref contentIndex, state);
        }
    }

    private static bool ShouldInclude(IElement element)
    {
        return element is not null && SupportedTags.Contains(element.TagName);
    }

    private static bool IsLineBreak(IElement element)
    {
        return string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Br, StringComparison.OrdinalIgnoreCase);
    }

    private static StyledElementFacts CreateFacts(IElement element)
    {
        var attributes = element.Attributes.ToDictionary(
            static attribute => attribute.Name,
            static attribute => attribute.Value,
            StringComparer.OrdinalIgnoreCase);

        return new StyledElementFacts(
            element.TagName,
            element.LocalName,
            element.GetAttribute(HtmlCssConstants.HtmlAttributes.Id),
            element.GetAttribute(HtmlCssConstants.HtmlAttributes.Class),
            attributes);
    }

    private sealed class TraversalIdentityState
    {
        private int _nextNodeId = 1;
        private int _nextContentId = 1;
        private int _nextSourceOrder = 1;

        public StyleSourceIdentity CreateRootIdentity(StyledElementFacts facts)
        {
            return CreateSourceIdentity(
                facts,
                parent: null,
                siblingIndex: 0,
                sourcePath: CreatePathSegment(facts, 0));
        }

        public StyleSourceIdentity CreateChildIdentity(
            StyleSourceIdentity parent,
            StyledElementFacts facts,
            int siblingIndex)
        {
            ArgumentNullException.ThrowIfNull(parent);

            return CreateSourceIdentity(
                facts,
                parent,
                siblingIndex,
                $"{parent.SourcePath}/{CreatePathSegment(facts, siblingIndex)}");
        }

        public StyleContentIdentity CreateContentIdentity(
            StyleSourceIdentity parent,
            string kind,
            int siblingIndex)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentException.ThrowIfNullOrWhiteSpace(kind);

            return new StyleContentIdentity(
                new StyleContentId(_nextContentId++),
                parent.NodeId,
                _nextSourceOrder++,
                siblingIndex,
                $"{parent.SourcePath}/{kind}[{siblingIndex}]");
        }

        private StyleSourceIdentity CreateSourceIdentity(
            StyledElementFacts facts,
            StyleSourceIdentity? parent,
            int siblingIndex,
            string sourcePath)
        {
            return new StyleSourceIdentity(
                new StyleNodeId(_nextNodeId++),
                parent?.NodeId,
                _nextSourceOrder++,
                siblingIndex,
                sourcePath,
                CreateElementIdentity(facts));
        }

        private static string CreatePathSegment(StyledElementFacts facts, int siblingIndex)
        {
            var tagName = ResolveTagName(facts);
            return $"{tagName}[{siblingIndex}]";
        }

        private static string? CreateElementIdentity(StyledElementFacts facts)
        {
            var tagName = ResolveTagName(facts);
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            var builder = new StringBuilder(tagName);
            var id = facts.Id;
            if (!string.IsNullOrWhiteSpace(id))
            {
                builder.Append('#');
                builder.Append(id.Trim());
            }

            var classAttribute = facts.ClassAttribute;
            if (!string.IsNullOrWhiteSpace(classAttribute))
            {
                foreach (var className in classAttribute.Split(
                    [' ', '\t', '\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    builder.Append('.');
                    builder.Append(className);
                }
            }

            return builder.ToString();
        }

        private static string ResolveTagName(StyledElementFacts facts)
        {
            if (!string.IsNullOrWhiteSpace(facts.LocalName))
            {
                return facts.LocalName.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(facts.TagName))
            {
                return facts.TagName.Trim().ToLowerInvariant();
            }

            return "element";
        }
    }
}
