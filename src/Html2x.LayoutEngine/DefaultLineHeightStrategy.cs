using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

public sealed class DefaultLineHeightStrategy(float minimumMultiplier = 1.2f) : ILineHeightStrategy
{
    public float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics)
    {
        var intrinsic = metrics.ascent + metrics.descent;
        var minimum = fontSizePt * minimumMultiplier;

        if (!float.IsFinite(intrinsic) || intrinsic <= 0)
        {
            return minimum;
        }

        return intrinsic < minimum ? minimum : intrinsic;
    }
}
