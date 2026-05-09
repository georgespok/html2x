using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style.Style;

internal static class InlineStyleSource
{
    public static string? GetDeclaration(IElement element, string propertyName) =>
        AuthoredCssDeclarationReader.GetDeclaration(element, propertyName)?.Text;

    public static string? GetValue(IElement element, string propertyName)
        => AuthoredCssDeclarationReader.GetValue(element, propertyName);
}
