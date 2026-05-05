using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
/// Provides font-accurate text measurement in points.
/// </summary>
public interface ITextMeasurer
{
    TextMeasurement Measure(FontKey font, float sizePt, string text);

    float MeasureWidth(FontKey font, float sizePt, string text);

    (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt);
}
