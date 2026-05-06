using System.Text;
using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style.Style;

/// <summary>
///     Walks supported DOM elements and materializes a StyleNode tree using the provided style factory.
/// </summary>
internal sealed class StyleTraversal
{
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

        var contentState = new ContentTraversalState();
        var childElementIndex = 0;
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                var textIdentity = state.CreateContentIdentity(
                    identity,
                    "text",
                    contentState.ReserveIndex());
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
                    contentState,
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
                contentState.ReserveIndex());
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

        return new(identity, facts, style, children, content);
    }

    private static void AppendUnsupportedContent(
        IElement element,
        StyleSourceIdentity owner,
        ICollection<StyleContentNode> content,
        ContentTraversalState contentState,
        TraversalIdentityState state)
    {
        foreach (var child in element.ChildNodes)
        {
            if (child.NodeType == NodeType.Text)
            {
                var contentIdentity = state.CreateContentIdentity(
                    owner,
                    "text",
                    contentState.ReserveIndex());
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
                    contentState.ReserveIndex());
                content.Add(StyleContentNode.ForLineBreak(contentIdentity));
            }

            AppendUnsupportedContent(childElement, owner, content, contentState, state);
        }
    }

    private static bool ShouldInclude(IElement? element) =>
        element is not null && HtmlCssConstants.SupportedElementTags.Contains(element.TagName);

    private static bool IsLineBreak(IElement element) => string.Equals(element.TagName, HtmlCssConstants.HtmlTags.Br,
        StringComparison.OrdinalIgnoreCase);

    private static StyledElementFacts CreateFacts(IElement element)
    {
        var attributes = element.Attributes.ToDictionary(
            static attribute => attribute.Name,
            static attribute => attribute.Value,
            StringComparer.OrdinalIgnoreCase);

        return new(
            element.TagName,
            element.LocalName,
            element.GetAttribute(HtmlCssConstants.HtmlAttributes.Id),
            element.GetAttribute(HtmlCssConstants.HtmlAttributes.Class),
            attributes);
    }

    private sealed class TraversalIdentityState
    {
        private int _nextContentId = 1;
        private int _nextNodeId = 1;
        private int _nextSourceOrder = 1;

        public StyleSourceIdentity CreateRootIdentity(StyledElementFacts facts) =>
            CreateSourceIdentity(
                facts,
                null,
                0,
                CreatePathSegment(facts, 0));

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
            string sourcePath) =>
            new StyleSourceIdentity(
                new StyleNodeId(_nextNodeId++),
                parent?.NodeId,
                _nextSourceOrder++,
                siblingIndex,
                sourcePath,
                CreateElementIdentity(facts));

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

    private sealed class ContentTraversalState
    {
        private int _contentIndex;

        public int ReserveIndex() => _contentIndex++;
    }
}
