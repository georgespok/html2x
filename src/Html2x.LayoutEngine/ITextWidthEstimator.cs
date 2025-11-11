using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine;

public interface ITextWidthEstimator
{
    float MeasureWidth(FontKey font, float fontSizePt, string text);
}
