using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.SkiaSharpIntegration;

public static class SkiaSharpHelpers
{
    public static void SkiaSharpSvgCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
    {
        container.Svg(size =>
        {
            using var stream = new MemoryStream();

            using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream))
                drawOnCanvas(canvas, size);
            
            var svgData = stream.ToArray();
            return Encoding.UTF8.GetString(svgData);
        });
    }
}