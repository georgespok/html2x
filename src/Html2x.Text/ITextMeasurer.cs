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
    /// <param name="font">The requested font family, weight, and style.</param>
    /// <param name="sizePt">The requested font size in points.</param>
    /// <param name="text">The exact text to measure. It may be empty.</param>
    /// <returns>Complete text measurement facts for the requested font and text.</returns>
    /// <remarks>
    ///     Implementations must return a non-null <see cref="TextMeasurement" /> with finite non-negative width,
    ///     ascent, and descent values. The returned resolved font must include a non-empty source id. Renderer font
    ///     file loadability is validated by the renderer.
    /// </remarks>
    TextMeasurement Measure(FontKey font, float sizePt, string text);
}
