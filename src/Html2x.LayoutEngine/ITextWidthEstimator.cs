using Html2x.Core.Layout;

namespace Html2x.LayoutEngine;

public interface ITextWidthEstimator
{
    float MeasureWidth(FontKey font, float fontSizePt, string text);
}
