using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Shouldly;
using System.Drawing;

namespace Html2x.LayoutEngine.Test.Geometry;

public sealed class BoxGeometryFactoryTests
{
    [Fact]
    public void FromBorderBox_InvalidLayoutInputs_NormalizesBeforePublishingGeometry()
    {
        var geometry = BoxGeometryFactory.FromBorderBox(
            float.NaN,
            float.PositiveInfinity,
            -10f,
            float.NegativeInfinity,
            new Spacing(),
            new Spacing(),
            baseline: float.NaN,
            markerOffset: -2f);

        geometry.BorderBoxRect.ShouldBe(new RectangleF(0f, 0f, 0f, 0f));
        geometry.ContentBoxRect.ShouldBe(new RectangleF(0f, 0f, 0f, 0f));
        geometry.Baseline.ShouldBeNull();
        geometry.MarkerOffset.ShouldBe(0f);
    }

    [Fact]
    public void WithBorderSize_ResizesThroughFactoryAndPreservesInsets()
    {
        var geometry = BoxGeometryFactory.FromBorderBox(
            10f,
            20f,
            100f,
            50f,
            new Spacing(3f, 4f, 5f, 6f),
            new Spacing(1f, 2f, 3f, 4f),
            baseline: 30f,
            markerOffset: 7f);

        var resized = BoxGeometryFactory.WithBorderSize(geometry, 30f, 12f);

        resized.BorderBoxRect.ShouldBe(new RectangleF(10f, 20f, 30f, 12f));
        resized.ContentBoxRect.ShouldBe(new RectangleF(20f, 24f, 14f, 0f));
        resized.Baseline.ShouldBe(30f);
        resized.MarkerOffset.ShouldBe(7f);
    }

    [Fact]
    public void ResolveContentWidth_BoxInsetsAndMarker_ReturnsAvailableWidth()
    {
        var padding = new Spacing(0f, 4f, 0f, 6f);
        var border = new Spacing(0f, 3f, 0f, 2f);

        var contentWidth = BoxGeometryFactory.ResolveContentWidth(
            borderWidth: 120f,
            padding,
            border,
            markerOffset: 7f);

        contentWidth.ShouldBe(98f);
    }

    [Theory]
    [InlineData(10f, 40f, 0f)]
    [InlineData(-10f, 0f, 0f)]
    public void ResolveContentWidth_InsufficientOrInvalidWidth_ClampsToZero(
        float borderWidth,
        float horizontalPadding,
        float expectedContentWidth)
    {
        var padding = new Spacing(0f, horizontalPadding / 2f, 0f, horizontalPadding / 2f);

        var contentWidth = BoxGeometryFactory.ResolveContentWidth(
            borderWidth,
            padding,
            new Spacing(),
            markerOffset: 4f);

        contentWidth.ShouldBe(expectedContentWidth);
    }

    [Fact]
    public void ResolveContentWidth_UnboundedBorderWidth_ReturnsUnboundedContentWidth()
    {
        var contentWidth = BoxGeometryFactory.ResolveContentWidth(
            float.PositiveInfinity,
            new Spacing(0f, 10f, 0f, 10f),
            new Spacing(0f, 2f, 0f, 2f),
            markerOffset: 6f);

        contentWidth.ShouldBe(float.PositiveInfinity);
    }
}
