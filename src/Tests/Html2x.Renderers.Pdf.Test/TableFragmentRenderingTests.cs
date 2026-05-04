using Html2x.RenderModel;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

[Trait("Category", "Integration")]
public sealed class TableFragmentRenderingTests
{
    [Fact]
    public void DrawPage_TableAndCellBorders_PaintExpectedColors()
    {
        var tableBorder = new ColorRgba(0xBE, 0x12, 0x3C, 0xFF);
        var cellBorder = new ColorRgba(0x1D, 0x4E, 0xD8, 0xFF);

        var page = CreatePage(
            new TableFragment([
                new TableRowFragment([
                    new TableCellFragment
                    {
                        Rect = new RectPt(20, 20, 80, 60),
                        ColumnIndex = 0,
                        Style = new VisualStyle(Borders: BorderEdges.Uniform(new BorderSide(2, cellBorder, BorderLineStyle.Solid)))
                    },
                    new TableCellFragment
                    {
                        Rect = new RectPt(100, 20, 80, 60),
                        ColumnIndex = 1,
                        Style = new VisualStyle(Borders: BorderEdges.Uniform(new BorderSide(2, cellBorder, BorderLineStyle.Solid)))
                    }
                ])
                {
                    Rect = new RectPt(20, 20, 160, 60),
                    RowIndex = 0
                }
            ])
            {
                Rect = new RectPt(20, 20, 160, 80),
                Style = new VisualStyle(Borders: BorderEdges.Uniform(new BorderSide(2, tableBorder, BorderLineStyle.Solid))),
                DerivedColumnCount = 2
            });

        using var bitmap = Draw(page);

        AssertColorClose(bitmap.GetPixel(100, 99), new SKColor(tableBorder.R, tableBorder.G, tableBorder.B, tableBorder.A));
        AssertColorClose(bitmap.GetPixel(99, 60), new SKColor(cellBorder.R, cellBorder.G, cellBorder.B, cellBorder.A));
    }

    [Fact]
    public void DrawPage_TableAndCellBackgrounds_PaintCellBeforeDescendants()
    {
        var tableBackground = new ColorRgba(0xE5, 0xE7, 0xEB, 0xFF);
        var cellBackground = new ColorRgba(0xBB, 0xF7, 0xD0, 0xFF);

        var page = CreatePage(
            new TableFragment([
                new TableRowFragment([
                    new TableCellFragment
                    {
                        Rect = new RectPt(20, 20, 80, 80),
                        ColumnIndex = 0,
                        Style = new VisualStyle(BackgroundColor: cellBackground)
                    },
                    new TableCellFragment
                    {
                        Rect = new RectPt(100, 20, 80, 80),
                        ColumnIndex = 1
                    }
                ])
                {
                    Rect = new RectPt(20, 20, 160, 80),
                    RowIndex = 0
                }
            ])
            {
                Rect = new RectPt(20, 20, 160, 80),
                Style = new VisualStyle(BackgroundColor: tableBackground),
                DerivedColumnCount = 2
            });

        using var bitmap = Draw(page);

        AssertColorClose(bitmap.GetPixel(60, 60), new SKColor(cellBackground.R, cellBackground.G, cellBackground.B, cellBackground.A));
        AssertColorClose(bitmap.GetPixel(140, 60), new SKColor(tableBackground.R, tableBackground.G, tableBackground.B, tableBackground.A));
    }

    [Fact]
    public void DrawPage_RowAndCellBackgrounds_PreferCellFillInsideCellBounds()
    {
        var rowBackground = new ColorRgba(0xD1, 0xD5, 0xDB, 0xFF);
        var cellBackground = new ColorRgba(0xFE, 0xF3, 0xC7, 0xFF);
        var borderColor = new ColorRgba(0x11, 0x18, 0x27, 0xFF);

        var page = CreatePage(
            new TableFragment([
                new TableRowFragment([
                    new TableCellFragment
                    {
                        Rect = new RectPt(20, 20, 80, 80),
                        ColumnIndex = 0,
                        Style = new VisualStyle(
                            BackgroundColor: cellBackground,
                            Borders: BorderEdges.Uniform(new BorderSide(2, borderColor, BorderLineStyle.Solid)))
                    },
                    new TableCellFragment
                    {
                        Rect = new RectPt(100, 20, 80, 80),
                        ColumnIndex = 1,
                        Style = new VisualStyle(
                            Borders: BorderEdges.Uniform(new BorderSide(2, borderColor, BorderLineStyle.Solid)))
                    }
                ])
                {
                    Rect = new RectPt(20, 20, 160, 80),
                    RowIndex = 0,
                    Style = new VisualStyle(BackgroundColor: rowBackground)
                }
            ])
            {
                Rect = new RectPt(20, 20, 160, 80),
                DerivedColumnCount = 2
            });

        using var bitmap = Draw(page);

        AssertColorClose(bitmap.GetPixel(60, 60), new SKColor(cellBackground.R, cellBackground.G, cellBackground.B, cellBackground.A));
        AssertColorClose(bitmap.GetPixel(140, 60), new SKColor(rowBackground.R, rowBackground.G, rowBackground.B, rowBackground.A));
        AssertColorClose(bitmap.GetPixel(99, 60), new SKColor(borderColor.R, borderColor.G, borderColor.B, borderColor.A));
    }

    private static LayoutPage CreatePage(Fragment fragment)
    {
        return new LayoutPage(
            new SizePt(200, 200),
            new Spacing(0, 0, 0, 0),
            new List<Fragment> { fragment },
            PageNumber: 1,
            PageBackground: new ColorRgba(255, 255, 255, 255));
    }

    private static SKBitmap Draw(LayoutPage page)
    {
        var options = new PdfRenderSettings();
        using var fontCache = new SkiaFontCache(new TestFileDirectory(), new TestSkiaTypefaceFactory());
        var commands = new PaintOrderResolver().Resolve(page);
        var drawer = new SkiaPaintCommandDrawer(options, fontCache);

        using var surface = SKSurface.Create(new SKImageInfo(200, 200, SKColorType.Bgra8888, SKAlphaType.Premul));
        drawer.Draw(surface.Canvas, commands);

        using var image = surface.Snapshot();
        var bitmap = new SKBitmap(image.Width, image.Height, image.ColorType, image.AlphaType);
        image.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0).ShouldBeTrue();
        return bitmap;
    }

    private static void AssertColorClose(SKColor actual, SKColor expected, byte tolerance = 20)
    {
        actual.Alpha.ShouldBeGreaterThan((byte)0);
        Math.Abs(actual.Red - expected.Red).ShouldBeLessThanOrEqualTo(tolerance);
        Math.Abs(actual.Green - expected.Green).ShouldBeLessThanOrEqualTo(tolerance);
        Math.Abs(actual.Blue - expected.Blue).ShouldBeLessThanOrEqualTo(tolerance);
    }
}
