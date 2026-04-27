using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Box;

internal static class HtmlElementClassifier
{
    public static bool IsImage(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Img);

    public static bool IsRule(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Hr);

    public static bool IsListContainer(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Ul) ||
        IsTag(element, HtmlCssConstants.HtmlTags.Ol);

    public static bool IsListItem(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Li);

    public static bool IsLineBreak(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Br);

    public static bool IsTableHeaderCell(IElement? element) =>
        IsTag(element, HtmlCssConstants.HtmlTags.Th);

    private static bool IsTag(IElement? element, string tagName)
    {
        return string.Equals(element?.TagName, tagName, StringComparison.OrdinalIgnoreCase);
    }
}
