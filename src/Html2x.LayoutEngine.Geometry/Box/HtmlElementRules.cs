namespace Html2x.LayoutEngine.Geometry.Box;

internal static class HtmlElementRules
{
    public static bool IsImage(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Img);

    public static bool IsRule(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Hr);

    public static bool IsListContainer(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Ul) ||
        IsTag(element, HtmlCssConstants.HtmlTags.Ol);

    public static bool IsListItem(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Li);

    public static bool IsLineBreak(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Br);

    public static bool IsTableHeaderCell(StyledElementFacts? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Th);

    private static bool IsTag(StyledElementFacts? element, string tagName) => element?.IsTag(tagName) == true;
}