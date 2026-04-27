using AngleSharp.Dom;
using Html2x.LayoutEngine.Models;

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

        return BuildNode(root, null, styleFactory);
    }

    private StyleNode BuildNode(IElement element, ComputedStyle? parent, Func<IElement, ComputedStyle?, ComputedStyle> styleFactory)
    {
        var style = styleFactory(element, parent);
        var node = new StyleNode
        {
            Element = element,
            Style = style
        };

        foreach (var child in element.Children)
        {
            if (!ShouldInclude(child))
            {
                continue;
            }

            var childNode = BuildNode(child, style, styleFactory);
            node.Children.Add(childNode);
        }

        return node;
    }

    private static bool ShouldInclude(IElement element)
    {
        return element is not null && SupportedTags.Contains(element.TagName);
    }
}
