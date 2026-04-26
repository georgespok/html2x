using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Test.TestDoubles;

/// <summary>
/// Provides deterministic text measurements for layout tests.
/// </summary>
public sealed class FakeTextMeasurer(float widthPerChar, float ascent, float descent) : ITextMeasurer
{
    public float MeasureWidth(FontKey font, float sizePt, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        return text.Length * widthPerChar;
    }

    public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
    {
        return (ascent, descent);
    }
}
