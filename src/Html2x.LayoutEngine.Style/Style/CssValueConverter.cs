using System.Globalization;
using AngleSharp.Css.Dom;

namespace Html2x.LayoutEngine.Style.Style;

internal sealed class CssValueConverter
{
    public string GetString(ICssStyleDeclaration styles, string property, string? fallback = null)
    {
        if (styles is null)
        {
            throw new ArgumentNullException(nameof(styles));
        }

        var value = styles.GetPropertyValue(property)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
    }

    public string NormalizeAlign(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.ToLowerInvariant();

    public bool IsBold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Equals(HtmlCssConstants.CssValues.Bold, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(value, out var weight) && weight >= 600;
    }

    public bool IsItalic(string? value) =>
        string.Equals(value, HtmlCssConstants.CssValues.Italic, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, HtmlCssConstants.CssValues.Oblique, StringComparison.OrdinalIgnoreCase);

    public float? ParseLengthPt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();

        if (trimmed.EndsWith(HtmlCssConstants.CssUnits.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var points)
                ? points
                : null;
        }

        if (trimmed.EndsWith(HtmlCssConstants.CssUnits.Px, StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels))
        {
            return CssUnitConversion.CssPxToPt(pixels);
        }

        if (string.Equals(trimmed, HtmlCssConstants.CssValues.Zero, StringComparison.OrdinalIgnoreCase))
        {
            return 0f;
        }

        return null;
    }
}