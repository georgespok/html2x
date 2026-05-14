using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style.Computation;

internal static class SupportedElementRules
{
    private static readonly IReadOnlySet<string> SupportedElementTags =
        new HashSet<string>(
            [
                HtmlCssVocabulary.HtmlTags.Body,
                HtmlCssVocabulary.HtmlTags.H1,
                HtmlCssVocabulary.HtmlTags.H2,
                HtmlCssVocabulary.HtmlTags.H3,
                HtmlCssVocabulary.HtmlTags.H4,
                HtmlCssVocabulary.HtmlTags.H5,
                HtmlCssVocabulary.HtmlTags.H6,
                HtmlCssVocabulary.HtmlTags.P,
                HtmlCssVocabulary.HtmlTags.Span,
                HtmlCssVocabulary.HtmlTags.Div,
                HtmlCssVocabulary.HtmlTags.Table,
                HtmlCssVocabulary.HtmlTags.Tbody,
                HtmlCssVocabulary.HtmlTags.Thead,
                HtmlCssVocabulary.HtmlTags.Tfoot,
                HtmlCssVocabulary.HtmlTags.Tr,
                HtmlCssVocabulary.HtmlTags.Td,
                HtmlCssVocabulary.HtmlTags.Th,
                HtmlCssVocabulary.HtmlTags.Img,
                HtmlCssVocabulary.HtmlTags.Hr,
                HtmlCssVocabulary.HtmlTags.Br,
                HtmlCssVocabulary.HtmlTags.Ul,
                HtmlCssVocabulary.HtmlTags.Ol,
                HtmlCssVocabulary.HtmlTags.Li,
                HtmlCssVocabulary.HtmlTags.Section,
                HtmlCssVocabulary.HtmlTags.Main,
                HtmlCssVocabulary.HtmlTags.Header,
                HtmlCssVocabulary.HtmlTags.Footer,
                HtmlCssVocabulary.HtmlTags.B,
                HtmlCssVocabulary.HtmlTags.I,
                HtmlCssVocabulary.HtmlTags.Strong,
                HtmlCssVocabulary.HtmlTags.U,
                HtmlCssVocabulary.HtmlTags.S
            ],
            StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(IElement? element) =>
        element is not null && SupportedElementTags.Contains(element.TagName);
}
