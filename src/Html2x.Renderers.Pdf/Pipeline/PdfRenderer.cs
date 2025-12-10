using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Drawing;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Pipeline;

/// <summary>
/// Renders an <see cref="HtmlLayout"/> to PDF using a SkiaSharp drawing pipeline.
/// </summary>
public class PdfRenderer
{
    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions? options = null, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(htmlLayout);
        options ??= new PdfOptions();

        var bytes = RenderWithSkia(htmlLayout, options, diagnosticsSession);
        return Task.FromResult(bytes);
    }

    private static byte[] RenderWithSkia(HtmlLayout layout, PdfOptions options, DiagnosticsSession? diagnosticsSession)
    {
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            throw new InvalidOperationException("Failed to create Skia PDF document.");
        }

        using var fontCache = new SkiaFontCache(options.FontPath);
        var drawer = new SkiaFragmentDrawer(options, diagnosticsSession, fontCache);

        foreach (var page in layout.Pages)
        {
            using var canvas = document.BeginPage(page.Size.Width, page.Size.Height);
            if (canvas is null)
            {
                continue;
            }

            drawer.DrawPage(canvas, page);
            document.EndPage();
        }

        document.Close();

        return stream.ToArray();
    }
}
