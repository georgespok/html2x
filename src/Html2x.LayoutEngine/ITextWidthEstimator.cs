using Html2x.RenderModel;
using Html2x.Text;

namespace Html2x.LayoutEngine;

public interface ITextWidthEstimator
{
    float MeasureWidth(FontKey font, float fontSizePt, string text);
}
