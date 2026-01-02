using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

/// <summary>
/// Default filter that includes the subset of tags currently supported by the layout pipeline.
/// </summary>
public sealed class DefaultStyleDomFilter : IStyleDomFilter
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
                HtmlCssConstants.HtmlTags.Tr,
                HtmlCssConstants.HtmlTags.Td,
                HtmlCssConstants.HtmlTags.Th,
                HtmlCssConstants.HtmlTags.Img,
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

    public bool ShouldInclude(IElement element)
    {
        return element is not null && SupportedTags.Contains(element.TagName);
    }
}
