using AngleSharp.Dom;

namespace Html2x.LayoutEngine.Style;

internal static class InlineStyleSource
{
    public static string? GetDeclaration(IElement element, string propertyName)
    {
        var style = element.GetAttribute(HtmlCssConstants.HtmlAttributes.Style);
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        var declarations = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var declaration in declarations)
        {
            var separatorIndex = declaration.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = declaration[..separatorIndex].Trim();
            if (string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return declaration.Trim();
            }
        }

        return null;
    }

    public static string? GetValue(IElement element, string propertyName)
    {
        var declaration = GetDeclaration(element, propertyName);
        if (declaration is null)
        {
            return null;
        }

        var separatorIndex = declaration.IndexOf(':', StringComparison.Ordinal);
        return separatorIndex < 0
            ? null
            : declaration[(separatorIndex + 1)..].Trim();
    }
}
