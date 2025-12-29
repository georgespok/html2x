using System.Drawing;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Options;
using Html2x.Abstractions.File;
using Html2x.Renderers.Pdf.Drawing;
using Moq;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

public sealed class BordersRenderingTests
{
    [Fact]
    public void DrawPage_BlockBorders_ShouldPaintExpectedColors()
    {
        var borders = new BorderEdges
        {
            Top = new BorderSide(4, new ColorRgba(0xBE, 0x12, 0x3C, 0xFF), BorderLineStyle.Solid),
            Right = new BorderSide(12, new ColorRgba(0x1D, 0x4E, 0xD8, 0xFF), BorderLineStyle.Dashed),
            Bottom = new BorderSide(6, new ColorRgba(0x0F, 0x17, 0x2A, 0xFF), BorderLineStyle.Dotted),
            Left = new BorderSide(2, new ColorRgba(0x16, 0xA3, 0x4A, 0xFF), BorderLineStyle.Solid)
        };

        var block = new BlockFragment
        {
            Rect = new RectangleF(20, 20, 160, 100),
            Style = new VisualStyle(Borders: borders)
        };

        var page = new LayoutPage(
            new SizeF(200, 200),
            new Spacing(0, 0, 0, 0),
            new List<Fragment> { block },
            PageNumber: 1,
            PageBackground: new ColorRgba(255, 255, 255, 255));

        var options = new PdfOptions { FontPath = string.Empty };
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        using var fontCache = new SkiaFontCache(fontPath: null, fileDirectory.Object);
        var drawer = new SkiaFragmentDrawer(options, diagnosticsSession: null, fontCache);

        using var surface = SKSurface.Create(new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        drawer.DrawPage(canvas, page);

        using var image = surface.Snapshot();
        using var bitmap = new SKBitmap(image.Width, image.Height, image.ColorType, image.AlphaType);
        image.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0).ShouldBeTrue();

        // Sample points are chosen at mid-stroke locations based on BorderShapeDrawer geometry.
        AssertColorClose(bitmap.GetPixel(100, 22), new SKColor(0xBE, 0x12, 0x3C, 0xFF)); // top
        AssertColorClose(bitmap.GetPixel(174, 34), new SKColor(0x1D, 0x4E, 0xD8, 0xFF)); // right (dashed, near start)
        AssertColorClose(bitmap.GetPixel(23, 117), new SKColor(0x0F, 0x17, 0x2A, 0xFF)); // bottom (dotted, near start)
        AssertColorClose(bitmap.GetPixel(21, 44), new SKColor(0x16, 0xA3, 0x4A, 0xFF)); // left
    }

    private static void AssertColorClose(SKColor actual, SKColor expected, byte tolerance = 30)
    {
        actual.Alpha.ShouldBeGreaterThan((byte)0);

        Math.Abs(actual.Red - expected.Red).ShouldBeLessThanOrEqualTo(tolerance);
        Math.Abs(actual.Green - expected.Green).ShouldBeLessThanOrEqualTo(tolerance);
        Math.Abs(actual.Blue - expected.Blue).ShouldBeLessThanOrEqualTo(tolerance);
    }
}
