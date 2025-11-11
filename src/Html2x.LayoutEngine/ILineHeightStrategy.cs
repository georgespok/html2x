using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

public interface ILineHeightStrategy
{
    float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics);
}
