using Html2x.RenderModel.Text;

namespace Html2x.LayoutEngine.Geometry;

internal interface ILineHeightStrategy
{
    float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics);
}