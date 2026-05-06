using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class UsedGeometryRulesTests
{
    [Fact]
    public void FromBorderBox_InvalidLayoutInputs_ThrowsBeforePublishingGeometry()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UsedGeometryRules.FromBorderBox(
            float.NaN,
            0f,
            10f,
            10f,
            new(),
            new()));
    }

    [Fact]
    public void WithBorderSize_ResizesThroughRulesAndPreservesInsets()
    {
        var geometry = UsedGeometryRules.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new(3f, 4f, 5f, 6f),
            new(1f, 2f, 3f, 4f),
            30f,
            7f);

        var resized = UsedGeometryRules.WithBorderSize(geometry, 30f, 12f);

        resized.BorderBoxRect.ShouldBe(new(10f, 20f, 30f, 12f));
        resized.ContentBoxRect.ShouldBe(new(20f, 24f, 14f, 0f));
        resized.Baseline.ShouldBe(30f);
        resized.MarkerOffset.ShouldBe(7f);
    }

    [Fact]
    public void ResolveContentFlowWidth_BoxInsetsAndMarker_ReturnsAvailableWidth()
    {
        var padding = new Spacing(0f, 4f, 0f, 6f);
        var border = new Spacing(0f, 3f, 0f, 2f);

        var contentWidth = UsedGeometryRules.ResolveContentFlowWidth(
            120f,
            padding,
            border,
            7f);

        contentWidth.ShouldBe(98f);
    }

    [Fact]
    public void ResolveContentFlowWidth_InsufficientWidth_ClampsToZero()
    {
        var padding = new Spacing(0f, 20f, 0f, 20f);

        var contentWidth = UsedGeometryRules.ResolveContentFlowWidth(
            10f,
            padding,
            new(),
            4f);

        contentWidth.ShouldBe(0f);
    }

    [Fact]
    public void ResolveContentFlowWidth_NegativeWidth_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UsedGeometryRules.ResolveContentFlowWidth(
            -10f,
            new(),
            new(),
            4f));
    }

    [Theory]
    [InlineData(10f, 40f, 0f)]
    public void ResolveContentFlowWidth_InsufficientWidthCases_ClampsToZero(
        float borderWidth,
        float horizontalPadding,
        float expectedContentWidth)
    {
        var padding = new Spacing(0f, horizontalPadding / 2f, 0f, horizontalPadding / 2f);

        var contentWidth = UsedGeometryRules.ResolveContentFlowWidth(
            borderWidth,
            padding,
            new(),
            4f);

        contentWidth.ShouldBe(expectedContentWidth);
    }

    [Fact]
    public void ResolveContentFlowWidth_UnboundedBorder_ReturnsUnbounded()
    {
        var contentWidth = UsedGeometryRules.ResolveContentFlowWidth(
            float.PositiveInfinity,
            new(0f, 10f, 0f, 10f),
            new(0f, 2f, 0f, 2f),
            6f);

        contentWidth.ShouldBe(float.PositiveInfinity);
    }
}