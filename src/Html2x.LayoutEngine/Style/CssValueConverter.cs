using System;
using System.Globalization;
using AngleSharp.Css.Dom;

namespace Html2x.LayoutEngine.Style;

public sealed class CssValueConverter : ICssValueConverter
{
    public string GetString(ICssStyleDeclaration styles, string property, string? fallback = null)
    {
        if (styles is null)
        {
            throw new ArgumentNullException(nameof(styles));
        }

        var value = styles.GetPropertyValue(property)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value!;
    }

    public string NormalizeAlign(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value!.ToLowerInvariant();
    }

    public bool IsBold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value!.Equals(HtmlCssConstants.CssValues.Bold, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return int.TryParse(value, out var weight) && weight >= 600;
    }

    public bool IsItalic(string? value)
    {
        return string.Equals(value, HtmlCssConstants.CssValues.Italic, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, HtmlCssConstants.CssValues.Oblique, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryGetLengthPt(string? raw, out float points)
    {
        points = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();

        if (trimmed.EndsWith(HtmlCssConstants.CssUnits.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out points);
        }

        if (trimmed.EndsWith(HtmlCssConstants.CssUnits.Px, StringComparison.OrdinalIgnoreCase) &&
            float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels))
        {
            points = (float)(pixels * (72.0 / 96.0));
            return true;
        }

        if (string.Equals(trimmed, HtmlCssConstants.CssValues.Zero, StringComparison.OrdinalIgnoreCase))
        {
            points = 0;
            return true;
        }

        return false;
    }

    public float GetLengthPt(ICssStyleDeclaration styles, string property, float fallback)
    {
        if (styles is null)
        {
            throw new ArgumentNullException(nameof(styles));
        }

        return TryGetLengthPt(styles.GetPropertyValue(property), out var points) ? points : fallback;
    }
}
