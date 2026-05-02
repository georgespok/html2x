using Html2x.RenderModel;
using Html2x.LayoutEngine.Models;
using Html2x.Text;

namespace Html2x.LayoutEngine;

internal interface ILineHeightStrategy
{
    float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics);
}
