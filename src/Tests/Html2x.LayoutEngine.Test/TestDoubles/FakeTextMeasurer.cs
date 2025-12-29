using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;

namespace Html2x.LayoutEngine.Test.TestDoubles;

public sealed class FakeTextMeasurer : ITextMeasurer
{
    private readonly float _widthPerChar;
    private readonly float _ascent;
    private readonly float _descent;

    public FakeTextMeasurer(float widthPerChar, float ascent, float descent)
    {
        _widthPerChar = widthPerChar;
        _ascent = ascent;
        _descent = descent;
    }

    public float MeasureWidth(FontKey font, float sizePt, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        return text.Length * _widthPerChar;
    }

    public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
    {
        return (_ascent, _descent);
    }
}
