using Html2x.Core.Layout;

namespace Html2x.Layout;

public interface ITextWidthEstimator
{
    float MeasureWidth(FontKey font, float fontSizePt, string text);
}
