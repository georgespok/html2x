using Html2x.Core.Layout;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

public sealed class DefaultLineHeightStrategy : ILineHeightStrategy
{
    private readonly float _minimumMultiplier;

    public DefaultLineHeightStrategy(float minimumMultiplier = 1.2f)
    {
        _minimumMultiplier = minimumMultiplier;
    }

    public float GetLineHeight(ComputedStyle style, FontKey font, float fontSizePt, (float ascent, float descent) metrics)
    {
        var intrinsic = metrics.ascent + metrics.descent;
        var minimum = fontSizePt * _minimumMultiplier;

        if (!float.IsFinite(intrinsic) || intrinsic <= 0)
        {
            return minimum;
        }

        return intrinsic < minimum ? minimum : intrinsic;
    }
}
