using System.Globalization;
using AngleSharp.Css.Dom;

namespace Html2x.LayoutEngine.Style.Computation;

internal sealed class CssValueParser
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

        if (value.Equals(HtmlCssVocabulary.CssValues.Bold, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(value, out var weight) && weight >= 600;
    }

    public bool IsItalic(string? value) =>
        string.Equals(value, HtmlCssVocabulary.CssValues.Italic, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, HtmlCssVocabulary.CssValues.Oblique, StringComparison.OrdinalIgnoreCase);

    public float? ParseLengthPt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();

        if (trimmed.EndsWith(HtmlCssVocabulary.CssUnits.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var points)
                ? points
                : null;
        }

        if (trimmed.EndsWith(HtmlCssVocabulary.CssUnits.Px, StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels))
        {
            return CssUnitConversion.CssPxToPt(pixels);
        }

        if (string.Equals(trimmed, HtmlCssVocabulary.CssValues.Zero, StringComparison.OrdinalIgnoreCase))
        {
            return 0f;
        }

        return null;
    }
}