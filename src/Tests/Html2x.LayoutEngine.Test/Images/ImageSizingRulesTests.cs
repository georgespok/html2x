using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Resources;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Box;

public sealed class ImageSizingRulesTests
{
    [Fact]
    public void ResolveImageLayout_CssWidthWithIntrinsicRatio_ComputesHeightFromIntrinsicAspect()
    {
        var sizingRules = CreateSizingRules(new(400d, 200d));
        var image = CreateImage(new()
        {
            WidthPt = 120f
        });

        var result = sizingRules.ResolveImageLayout(image, 500f);

        result.ContentWidth.ShouldBe(120f, 0.01f);
        result.ContentHeight.ShouldBe(60f, 0.01f);
        result.BorderBoxWidth.ShouldBe(120f, 0.01f);
        result.BorderBoxHeight.ShouldBe(60f, 0.01f);
    }

    [Fact]
    public void ResolveImageLayout_CssDimensions_OverrideIntrinsicRatio()
    {
        var sizingRules = CreateSizingRules(new(400d, 200d));
        var image = CreateImage(new()
        {
            WidthPt = 80f,
            HeightPt = 40f
        });

        var result = sizingRules.ResolveImageLayout(image, 500f);

        result.ContentWidth.ShouldBe(80f, 0.01f);
        result.ContentHeight.ShouldBe(40f, 0.01f);
    }

    [Fact]
    public void ResolveImageLayout_IntrinsicSizeAboveAvailableWidth_ScalesPreservingAspect()
    {
        var sizingRules = CreateSizingRules(new(300d, 150d));
        var image = CreateImage(new());

        var result = sizingRules.ResolveImageLayout(image, 150f);

        result.ContentWidth.ShouldBe(150f, 0.01f);
        result.ContentHeight.ShouldBe(75f, 0.01f);
    }

    [Fact]
    public void ResolveImageLayout_PaddingAndBorder_InflateTotalSize()
    {
        var sizingRules = CreateSizingRules(new(100d, 50d));
        var image = CreateImage(new()
        {
            Padding = new(2f, 3f, 4f, 5f),
            Borders = BorderEdges.Uniform(new(1f, ColorRgba.Black, BorderLineStyle.Solid))
        });

        var result = sizingRules.ResolveImageLayout(image, 500f);

        result.ContentWidth.ShouldBe(75f, 0.01f);
        result.ContentHeight.ShouldBe(37.5f, 0.01f);
        result.BorderBoxWidth.ShouldBe(85f, 0.01f);
        result.BorderBoxHeight.ShouldBe(45.5f, 0.01f);
    }

    [Theory]
    [InlineData(ImageLoadStatus.Missing, true, false)]
    [InlineData(ImageLoadStatus.Oversized, false, true)]
    [InlineData(ImageLoadStatus.InvalidDataUri, true, false)]
    [InlineData(ImageLoadStatus.DecodeFailed, true, false)]
    [InlineData(ImageLoadStatus.OutOfScope, true, false)]
    public void ResolveImageLayout_MetadataStatus_CarriesCanonicalStatus(
        ImageLoadStatus status,
        bool expectedMissing,
        bool expectedOversized)
    {
        var sizingRules = CreateSizingRules(new(100d, 50d), status);
        var image = CreateImage(new());

        var result = sizingRules.ResolveImageLayout(image, 500f);

        result.Status.ShouldBe(status);
        ImageLoadStatusFacts.IsMissing(result.Status).ShouldBe(expectedMissing);
        ImageLoadStatusFacts.IsOversized(result.Status).ShouldBe(expectedOversized);
    }

    private static ImageSizingRules CreateSizingRules(
        SizePx intrinsicSize,
        ImageLoadStatus status = ImageLoadStatus.Ok) =>
        new(new()
        {
            ImageMetadataResolver = new FixedImageMetadataResolver(intrinsicSize, status)
        });

    private static ImageBox CreateImage(ComputedStyle style) =>
        new(BoxRole.Block)
        {
            Src = "image.png",
            Style = style
        };

    private sealed class FixedImageMetadataResolver(
        SizePx intrinsicSize,
        ImageLoadStatus status = ImageLoadStatus.Ok) : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src) =>
            new()
            {
                Src = src,
                Status = status,
                IntrinsicSizePx = intrinsicSize
            };
    }
}
