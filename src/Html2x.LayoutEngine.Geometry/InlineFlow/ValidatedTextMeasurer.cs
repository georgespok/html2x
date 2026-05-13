using Html2x.RenderModel.Text;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

internal sealed class ValidatedTextMeasurer(ITextMeasurer inner) : ITextMeasurer
{
    private readonly ITextMeasurer _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public TextMeasurement Measure(FontKey font, float sizePt, string text)
    {
        var measurement = _inner.Measure(font, sizePt, text);
        return measurement ?? throw new InvalidOperationException("ITextMeasurer.Measure returned null.");
    }
}
