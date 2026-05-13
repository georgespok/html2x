namespace Html2x.LayoutEngine.Geometry.Style;

internal static class HtmlElementRules
{
    public static bool IsImage(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Img);

    public static bool IsRule(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Hr);

    public static bool IsListContainer(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Ul) ||
        IsTag(element, HtmlCssVocabulary.HtmlTags.Ol);

    public static bool IsListItem(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Li);

    public static bool IsLineBreak(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Br);

    public static bool IsTableHeaderCell(StyledElementFacts? element) =>
        IsTag(element, HtmlCssVocabulary.HtmlTags.Th);

    private static bool IsTag(StyledElementFacts? element, string tagName) => element?.IsTag(tagName) == true;
}
