using Html2x.RenderModel.Text;

namespace Html2x.Text;

/// <summary>
///     Provides font-accurate text measurement in points.
/// </summary>
public interface ITextMeasurer
{
    /// <summary>
    ///     Measures text and returns complete width, metrics, and resolved font facts for layout and rendering.
    /// </summary>
    /// <remarks>
    ///     Implementations must return a non-null <see cref="TextMeasurement" /> with finite non-negative width,
    ///     ascent, and descent values. The returned resolved font must include a non-empty source id. Renderer font
    ///     file loadability is validated by the renderer.
    /// </remarks>
    TextMeasurement Measure(FontKey font, float sizePt, string text);
}
