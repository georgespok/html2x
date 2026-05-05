using Html2x.Renderers.Pdf.Drawing;
using Html2x.RenderModel.Styles;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

public class BorderShapeDrawerTests
{
    private readonly BorderShapeDrawer _sut = new();

    [Theory]
    [MemberData(nameof(CalculateRectCases))]
    public void CalculateRects_SizeAndBorders_ReturnsExpectedRects(
        SKSize size,
        BorderEdges borders,
        SKRect expectedTop,
        SKRect expectedRight,
        SKRect expectedBottom,
        SKRect expectedLeft)
    {
        var (top, right, bottom, left) = _sut.CalculateRects(size, borders);

        top.ShouldBe(expectedTop);
        right.ShouldBe(expectedRight);
        bottom.ShouldBe(expectedBottom);
        left.ShouldBe(expectedLeft);
    }

    [Fact]
    public void Draw_DashedBorder_RendersGaps()
    {
        var size = new SKSize(100, 100);
        var red = new ColorRgba(255, 0, 0, 255);
        var width = 10f;
        var borders = new BorderEdges
        {
            Top = new BorderSide(width, red, BorderLineStyle.Dashed)
        };

        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        _sut.Draw(canvas, size, borders);

        var pixelFilled = bitmap.GetPixel(10, 5);
        ((int)pixelFilled.Red).ShouldBe(255);
        ((int)pixelFilled.Alpha).ShouldBe(255);

        var pixelGap = bitmap.GetPixel(35, 5);
        ((int)pixelGap.Alpha).ShouldBe(0, "Pixel at 35,5 should be transparent (gap) for dashed line");
    }

    [Fact]
    public void Draw_TinyBoxAndLargeBorders_DoesNotThrow()
    {
        var size = new SKSize(2, 2);
        var borders = BorderEdges.Uniform(new BorderSide(8, ColorRgba.Black, BorderLineStyle.Solid));

        using var bitmap = new SKBitmap(2, 2);
        using var canvas = new SKCanvas(bitmap);

        Should.NotThrow(() => _sut.Draw(canvas, size, borders));
    }

    public static IEnumerable<object[]> CalculateRectCases()
    {
        yield return
        [
            new SKSize(100, 100),
            BorderEdges.Uniform(new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid)),
            new SKRect(0, 0, 100, 10),
            new SKRect(90, 10, 100, 90),
            new SKRect(0, 90, 100, 100),
            new SKRect(0, 10, 10, 90)
        ];

        yield return
        [
            new SKSize(100, 100),
            new BorderEdges
            {
                Top = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid),
                Right = null,
                Bottom = new BorderSide(0, ColorRgba.Black, BorderLineStyle.Solid),
                Left = null
            },
            new SKRect(0, 0, 100, 10),
            new SKRect(100, 10, 100, 100),
            new SKRect(0, 100, 100, 100),
            new SKRect(0, 10, 0, 100)
        ];

        yield return
        [
            new SKSize(30, 10),
            new BorderEdges
            {
                Top = new BorderSide(8, ColorRgba.Black, BorderLineStyle.Solid),
                Right = new BorderSide(4, ColorRgba.Black, BorderLineStyle.Solid),
                Bottom = new BorderSide(8, ColorRgba.Black, BorderLineStyle.Solid),
                Left = new BorderSide(4, ColorRgba.Black, BorderLineStyle.Solid)
            },
            new SKRect(0, 0, 30, 8),
            new SKRect(26, 8, 30, 8),
            new SKRect(0, 2, 30, 10),
            new SKRect(0, 8, 4, 8)
        ];

        yield return
        [
            new SKSize(10, 30),
            new BorderEdges
            {
                Top = new BorderSide(4, ColorRgba.Black, BorderLineStyle.Solid),
                Right = new BorderSide(20, ColorRgba.Black, BorderLineStyle.Solid),
                Bottom = new BorderSide(4, ColorRgba.Black, BorderLineStyle.Solid),
                Left = new BorderSide(20, ColorRgba.Black, BorderLineStyle.Solid)
            },
            new SKRect(0, 0, 10, 4),
            new SKRect(0, 4, 10, 26),
            new SKRect(0, 26, 10, 30),
            new SKRect(0, 4, 10, 26)
        ];
    }
}
