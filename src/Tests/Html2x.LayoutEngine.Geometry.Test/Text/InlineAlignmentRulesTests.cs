using Html2x.LayoutEngine.Geometry.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public sealed class InlineAlignmentRulesTests
{
    private readonly InlineAlignmentRules _resolver = new();

    [Theory]
    [InlineData("left", 120f, 80f, 0f)]
    [InlineData("center", 120f, 80f, 20f)]
    [InlineData("right", 120f, 80f, 40f)]
    [InlineData("justify", 120f, 80f, 0f)]
    public void ResolveLineOffset_WithSupportedAlignment_ReturnsExpectedOffset(
        string textAlign,
        float contentWidth,
        float lineWidth,
        float expected)
    {
        var line = CreateLine(lineWidth);

        var offset = _resolver.ResolveLineOffset(textAlign, contentWidth, lineWidth, line, 0, 2);

        offset.ShouldBe(expected);
    }

    [Theory]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void ResolveLineOffset_WithInvalidContentWidth_ReturnsZero(float contentWidth)
    {
        var line = CreateLine(80f);

        var offset = _resolver.ResolveLineOffset("right", contentWidth, 80f, line, 0, 2);

        offset.ShouldBe(0f);
    }

    [Fact]
    public void JustifyLine_ForLastLine_ReturnsFalse()
    {
        _resolver.ShouldJustifyLine(CreateLine(80f), 1, 2).ShouldBeFalse();
    }

    private static TextLayoutLine CreateLine(float lineWidth) => new([], lineWidth, 12f);
}