using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Drawing;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

public class BorderShapeDrawerTests
{
    private readonly BorderShapeDrawer _sut = new();

    [Fact]
    public void CalculateRects_CalculatesCorrectOverlap()
    {
        // Arrange
        var size = new SKSize(100, 100);
        var borders = new BorderEdges
        {
            Top = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid),
            Right = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid),
            Bottom = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid),
            Left = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid)
        };

        // Act
        var (top, right, bottom, left) = _sut.CalculateRects(size, borders);

        // Assert
        // Top: Full width (0,0, 100, 10)
        top.Left.ShouldBe(0);
        top.Top.ShouldBe(0);
        top.Right.ShouldBe(100);
        top.Bottom.ShouldBe(10);

        // Right: Height minus Top/Bottom (90, 10, 100, 90)
        right.Left.ShouldBe(90);
        right.Top.ShouldBe(10);
        right.Right.ShouldBe(100);
        right.Bottom.ShouldBe(90);

        // Bottom rect: Full width (0, 90, 100, 100)
        bottom.Left.ShouldBe(0);
        bottom.Top.ShouldBe(90);
        bottom.Right.ShouldBe(100);
        bottom.Bottom.ShouldBe(100);

        // Left rect: Height minus Top/Bottom (0, 10, 10, 90)
        left.Left.ShouldBe(0);
        left.Top.ShouldBe(10);
        left.Right.ShouldBe(10);
        left.Bottom.ShouldBe(90);
    }

    [Fact]
    public void Draw_DashedBorder_RendersGaps()
    {
        // Arrange
        var size = new SKSize(100, 100);
        var red = new ColorRgba(255, 0, 0, 255);
        var width = 10f;
        var borders = new BorderEdges
        {
            Top = new BorderSide(width, red, BorderLineStyle.Dashed)
        };

        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        
        // Act
        _sut.Draw(canvas, size, borders);

        // Assert
        // Dash pattern is [30, 10] (3x width dash, 1x width gap).
        // Starts at 0.
        // 0-30: Filled (Red)
        // 30-40: Gap (Transparent)
        // 40-70: Filled (Red)
        
        // Check filled pixel at 10, 5
        var pixelFilled = bitmap.GetPixel(10, 5);
        ((int)pixelFilled.Red).ShouldBe(255);
        ((int)pixelFilled.Alpha).ShouldBe(255);

        // Check gap pixel at 35, 5
        var pixelGap = bitmap.GetPixel(35, 5);
        ((int)pixelGap.Alpha).ShouldBe(0, "Pixel at 35,5 should be transparent (gap) for dashed line");
    }

    [Fact]
    public void CalculateRects_WithZeroWidthSides_HandlesCorrectly()
    {
        // Arrange
        var size = new SKSize(100, 100);
        // Only top border, others 0/null
        var borders = new BorderEdges
        {
            Top = new BorderSide(10, ColorRgba.Black, BorderLineStyle.Solid),
            Right = null,
            Bottom = new BorderSide(0, ColorRgba.Black, BorderLineStyle.Solid),
            Left = null
        };

        // Act
        var (top, right, bottom, left) = _sut.CalculateRects(size, borders);

        // Assert
        // Top: Full width (0, 0, 100, 10)
        top.Height.ShouldBe(10);

        // Right: (100, 10, 100, 100) -> Width 0, Height 90 (100 - 10 - 0)
        right.Width.ShouldBe(0);
        right.Height.ShouldBe(90);

        // Bottom: (0, 100, 100, 100) -> Height 0
        bottom.Height.ShouldBe(0);

        // Left: (0, 10, 0, 100) -> Width 0, Height 90
        left.Width.ShouldBe(0);
        left.Height.ShouldBe(90);
    }
}