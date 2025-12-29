using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Text;

/// <summary>
/// Provides font-accurate text measurement in points.
/// </summary>
public interface ITextMeasurer
{
    float MeasureWidth(FontKey font, float sizePt, string text);

    (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt);
}
