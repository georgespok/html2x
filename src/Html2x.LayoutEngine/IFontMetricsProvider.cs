using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine;

/// <summary>
/// Provides font metrics and crude width estimation for layout calculations.
/// </summary>
public interface IFontMetricsProvider
{
    FontKey GetFontKey(ComputedStyle style);

    float GetFontSize(ComputedStyle style);

    (float ascent, float descent) GetMetrics(FontKey font, float sizePt);

    float MeasureTextWidth(FontKey font, float sizePt, string text);
}
