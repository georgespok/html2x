using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.RenderModel.Geometry;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test.Drawing;

public sealed class SkiaPaintCommandDrawerTests
{
    [Fact]
    public void Draw_UnknownPaintCommand_ThrowsWithExplicitGuidance()
    {
        using var fontCache = new SkiaFontCache(new TestFileSystemReader(), new TestSkiaTypefaceFactory());
        var drawer = new SkiaPaintCommandDrawer(new(), fontCache);
        using var bitmap = new SKBitmap(1, 1);
        using var canvas = new SKCanvas(bitmap);

        var exception = Should.Throw<NotSupportedException>(() =>
            drawer.Draw(canvas, [new CustomPaintCommand()]));

        exception.Message.ShouldContain(nameof(CustomPaintCommand));
        exception.Message.ShouldContain("Unsupported paint command type");
    }

    private sealed record CustomPaintCommand() : PaintCommand(
        PaintCommandKind.Background,
        1,
        1,
        new RectPt(0, 0, 1, 1),
        0,
        0);
}
