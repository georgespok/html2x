using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine;

public sealed class DefaultLineHeightStrategy(float minimumMultiplier = 1.2f) : ILineHeightStrategy
{
    public float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics)
    {
        var intrinsic = metrics.ascent + metrics.descent;
        var minimum = fontSizePt * minimumMultiplier;

        var baseline = !float.IsFinite(intrinsic) || intrinsic <= 0
            ? minimum
            : Math.Max(intrinsic, minimum);

        var candidate = style.LineHeightMultiplier * fontSizePt;
        if (!float.IsFinite(candidate) || candidate <= 0)
        {
            return baseline;
        }

        return Math.Max(candidate, baseline);
    }
}
