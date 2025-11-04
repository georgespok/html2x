using Html2x.Core.Layout;
using Html2x.Layout.Style;

namespace Html2x.Layout;

public interface ILineHeightStrategy
{
    float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics);
}
