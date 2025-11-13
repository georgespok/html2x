using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine;

/// <summary>
///     Default heuristic font metrics provider. Replace with a real font engine when available.
/// </summary>
public sealed class FontMetricsProvider : IFontMetricsProvider
{
    public FontKey GetFontKey(ComputedStyle style)
    {
        if (style is null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        var weight = style.Bold ? FontWeight.W700 : FontWeight.W400;
        var fontStyle = style.Italic ? FontStyle.Italic : FontStyle.Normal;
        var family = string.IsNullOrWhiteSpace(style.FontFamily)
            ? HtmlCssConstants.Defaults.FontFamily
            : style.FontFamily;

        return new FontKey(family, weight, fontStyle);
    }

    public float GetFontSize(ComputedStyle style)
    {
        if (style is null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        return style.FontSizePt > 0
            ? style.FontSizePt
            : HtmlCssConstants.Defaults.DefaultFontSizePt;
    }

    public (float ascent, float descent) GetMetrics(FontKey font, float sizePt)
    {
        // Roughly 80/20 rule: 80% up, 20% down.
        var ascent = sizePt * 0.8f;
        var descent = sizePt * 0.2f;
        return (ascent, descent);
    }

    public float MeasureTextWidth(FontKey font, float sizePt, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        // Simplistic width estimation until proper glyph measurement is plugged in.
        return text.Length * sizePt * 0.5f;
    }
}