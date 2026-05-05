using Html2x.RenderModel.Text;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
/// Provides font metrics and crude width estimation for layout calculations.
/// </summary>
internal interface IFontMetricsProvider
{
    FontKey GetFontKey(ComputedStyle style);

    float GetFontSize(ComputedStyle style);

    (float ascent, float descent) GetMetrics(FontKey font, float sizePt);

    float MeasureTextWidth(FontKey font, float sizePt, string text);
}
