using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Box;

public sealed class ImageLayoutResolverTests
{
    [Fact]
    public void Resolve_CssWidthWithIntrinsicRatio_ComputesHeightFromIntrinsicAspect()
    {
        var resolver = CreateResolver(new SizePx(400d, 200d));
        var image = CreateImage(new ComputedStyle
        {
            WidthPt = 120f
        });

        var result = resolver.Resolve(image, availableWidth: 500f);

        result.ContentWidth.ShouldBe(120f, 0.01f);
        result.ContentHeight.ShouldBe(60f, 0.01f);
        result.TotalWidth.ShouldBe(120f, 0.01f);
        result.TotalHeight.ShouldBe(60f, 0.01f);
    }

    [Fact]
    public void Resolve_CssDimensions_OverrideIntrinsicRatio()
    {
        var resolver = CreateResolver(new SizePx(400d, 200d));
        var image = CreateImage(new ComputedStyle
        {
            WidthPt = 80f,
            HeightPt = 40f
        });

        var result = resolver.Resolve(image, availableWidth: 500f);

        result.ContentWidth.ShouldBe(80f, 0.01f);
        result.ContentHeight.ShouldBe(40f, 0.01f);
    }

    [Fact]
    public void Resolve_IntrinsicSizeAboveAvailableWidth_ScalesPreservingAspect()
    {
        var resolver = CreateResolver(new SizePx(300d, 150d));
        var image = CreateImage(new ComputedStyle());

        var result = resolver.Resolve(image, availableWidth: 150f);

        result.ContentWidth.ShouldBe(150f, 0.01f);
        result.ContentHeight.ShouldBe(75f, 0.01f);
    }

    [Fact]
    public void Resolve_PaddingAndBorder_InflateTotalSize()
    {
        var resolver = CreateResolver(new SizePx(100d, 50d));
        var image = CreateImage(new ComputedStyle
        {
            Padding = new Spacing(2f, 3f, 4f, 5f),
            Borders = BorderEdges.Uniform(new BorderSide(1f, ColorRgba.Black, BorderLineStyle.Solid))
        });

        var result = resolver.Resolve(image, availableWidth: 500f);

        result.ContentWidth.ShouldBe(75f, 0.01f);
        result.ContentHeight.ShouldBe(37.5f, 0.01f);
        result.TotalWidth.ShouldBe(85f, 0.01f);
        result.TotalHeight.ShouldBe(45.5f, 0.01f);
    }

    private static ImageLayoutResolver CreateResolver(SizePx intrinsicSize)
    {
        return new ImageLayoutResolver(new LayoutGeometryRequest
        {
            ImageProvider = new FixedImageProvider(intrinsicSize)
        });
    }

    private static ImageBox CreateImage(ComputedStyle style)
    {
        return new ImageBox(BoxRole.Block)
        {
            Src = "image.png",
            Style = style
        };
    }

    private sealed class FixedImageProvider(SizePx intrinsicSize) : IImageProvider
    {
        public ImageLoadResult Load(string src, string baseDirectory, long maxBytes)
        {
            return new ImageLoadResult
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = intrinsicSize
            };
        }
    }
}
