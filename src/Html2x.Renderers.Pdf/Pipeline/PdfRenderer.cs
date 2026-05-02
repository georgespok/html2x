using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.Text;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Pipeline;

/// <summary>
/// Renders an <see cref="HtmlLayout"/> to PDF using a SkiaSharp drawing pipeline.
/// The renderer owns paint output only and treats layout pages and fragments as read-only inputs.
/// </summary>
public class PdfRenderer
{
    private readonly IFileDirectory _fileDirectory;
    private readonly ISkiaTypefaceFactory _typefaceFactory;

    public PdfRenderer()
        : this(new FileDirectory(), new SkiaTypefaceFactory())
    {
    }

    internal PdfRenderer(IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory)
    {
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
    }

    public Task<byte[]> RenderAsync(
        HtmlLayout htmlLayout,
        PdfRenderSettings? settings = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(htmlLayout);
        settings ??= new PdfRenderSettings();

        var bytes = RenderWithSkia(htmlLayout, settings, diagnosticsSink);
        return Task.FromResult(bytes);
    }

    private byte[] RenderWithSkia(
        HtmlLayout layout,
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink)
    {
        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            throw new InvalidOperationException("Failed to create Skia PDF document.");
        }

        using var fontCache = new SkiaFontCache(_fileDirectory, _typefaceFactory);
        var paintOrder = new PaintOrderResolver();
        var drawer = new SkiaPaintCommandDrawer(settings, fontCache, diagnosticsSink);

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
