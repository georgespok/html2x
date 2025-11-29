using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class DefaultLineHeightStrategyTests
{
    [Theory]
    [InlineData(1.1f, 6f, 2f, 10f, 1.2f, 12f)]  // multiplier below minimum -> minimum applied
    [InlineData(1.5f, 5f, 3f, 10f, 1.2f, 15f)]  // multiplier wins when above baseline
    [InlineData(2.0f, 0f, 0f, 10f, 1.2f, 20f)]  // no intrinsic metrics, multiplier used
    [InlineData(-1f, 6f, 2f, 10f, 1.2f, 12f)]   // negative multiplier ignored -> baseline
    public void GetLineHeight_WithMultiplier_ComputesExpected(
        float multiplier,
        float ascent,
        float descent,
        float fontSize,
        float minimumMultiplier,
        float expected)
    {
        var style = new ComputedStyle { LineHeightMultiplier = multiplier };
        var sut = new DefaultLineHeightStrategy(minimumMultiplier);

        var height = sut.GetLineHeight(
            style,
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            fontSize,
            (ascent, descent));

        height.ShouldBe(expected);
    }

    [Theory]
    [InlineData(8f, 3f, 10f, 1.2f, 12f)]         // baseline uses minimum when intrinsic below
    [InlineData(9f, 5f, 10f, 1.2f, 14f)]         // intrinsic above minimum
    [InlineData(float.NaN, float.NaN, 10f, 1.2f, 12f)] // non-finite intrinsic -> minimum
    public void GetLineHeight_WithoutMultiplier_UsesBaseline(
        float ascent,
        float descent,
        float fontSize,
        float minimumMultiplier,
        float expected)
    {
        var style = new ComputedStyle();
        var sut = new DefaultLineHeightStrategy(minimumMultiplier);

        var height = sut.GetLineHeight(
            style,
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            fontSize,
            (ascent, descent));

        height.ShouldBe(expected);
    }
}
