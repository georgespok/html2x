using SkiaSharp;
using Xunit.Abstractions;

namespace Html2x.Renderers.Pdf.Test;

public class AbsolutePositioningTests(ITestOutputHelper output)
{
    [Fact]
    public void AbsoluteRendering_WithSkiaPdf_ShouldGeneratePdf()
    {
        // Arrange: mock fragments
        var fragments = new ITestFragment[]
        {
            new TestRectFragment(50, 40, 200, 60, SKColors.LightGray),
            new TestTextFragment(60, 80, "Hello absolute world!", 20, SKColors.Black),
            new TestRectFragment(100, 150, 150, 40, SKColors.CornflowerBlue)
        };

        // Prepare output path
        var tempDir = Path.GetTempPath();
        var outputPath = Path.Combine(tempDir, "absolute-rendering.pdf");

        using var pdfStream = File.OpenWrite(outputPath);
        using var document = SKDocument.CreatePdf(pdfStream);

        // A4: 595×842 points
        const float width = 595;
        const float height = 842;

        using (var canvas = document.BeginPage(width, height))
        {
            canvas.Clear(SKColors.White);

            foreach (var f in fragments)
            {
                canvas.Save();
                canvas.Translate(f.X, f.Y);

                switch (f)
                {
                    case TestRectFragment rect:
                        DrawRect(canvas, rect);
                        break;

                    case TestTextFragment text:
                        DrawText(canvas, text);
                        break;
                }

                canvas.Restore();
            }

            document.EndPage();
        }

        document.Close();

        // Assert that PDF exists and has content
        Assert.True(new FileInfo(outputPath).Length > 1000);

        output.WriteLine($"PDF saved to: {outputPath}");
    }

    // ---------------- Skia Drawing Methods ----------------

    private static void DrawRect(SKCanvas canvas, TestRectFragment rect)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            
            Color = rect.Color,
            IsAntialias = true
        };

        canvas.DrawRect(0, 0, rect.Width, rect.Height, paint);
    }

    private static void DrawText(SKCanvas canvas, TestTextFragment text)
    {
        using var paint = new SKPaint
        {
            Color = text.Color,
            TextSize = text.FontSize,
            IsAntialias = true
        };

        // Draw text baseline at FontSize (like top-left baseline)
        canvas.DrawText(text.Text, 0, text.FontSize, paint);
    }

    // ---------------- Fragment Model ----------------

    private interface ITestFragment
    {
        float X { get; }
        float Y { get; }
    }

    private record TestRectFragment(float X, float Y, float Width, float Height, SKColor Color)
        : ITestFragment;

    private record TestTextFragment(float X, float Y, string Text, float FontSize, SKColor Color)
        : ITestFragment;
}
