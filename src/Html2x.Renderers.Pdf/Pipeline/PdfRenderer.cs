using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Pipeline;

/// <summary>
/// Renders an <see cref="HtmlLayout"/> to PDF using a SkiaSharp drawing pipeline.
/// The renderer owns paint output only and treats layout pages and fragments as read-only inputs.
/// </summary>
public class PdfRenderer
{
    private readonly IFileDirectory _fileDirectory;

    public PdfRenderer(IFileDirectory fileDirectory)
    {
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
    }

    public Task<byte[]> RenderAsync(
        HtmlLayout htmlLayout,
        PdfOptions? options = null,
        IFontSource? fontSource = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(htmlLayout);
        options ??= new PdfOptions();

        var bytes = RenderWithSkia(htmlLayout, options, fontSource, diagnosticsSink);
        return Task.FromResult(bytes);
    }

    private byte[] RenderWithSkia(
        HtmlLayout layout,
        PdfOptions options,
        IFontSource? fontSource,
        IDiagnosticsSink? diagnosticsSink)
    {
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            throw new InvalidOperationException("Failed to create Skia PDF document.");
        }

        using var fontCache = new SkiaFontCache(options.FontPath, _fileDirectory, fontSource);
        var paintOrder = new PaintOrderResolver();
        var drawer = new SkiaPaintCommandDrawer(options, fontCache, diagnosticsSink);

        foreach (var page in layout.Pages)
        {
            var size = page.PageSize;
            using var canvas = document.BeginPage(size.Width, size.Height);
            if (canvas is null)
            {
                continue;
            }

            var commands = paintOrder.Resolve(page);
            drawer.Draw(canvas, commands);
            document.EndPage();
        }

        document.Close();

        return stream.ToArray();
    }
}
