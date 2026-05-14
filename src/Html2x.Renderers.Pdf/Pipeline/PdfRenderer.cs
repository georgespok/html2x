using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.RenderModel.Documents;
using Html2x.Text;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Pipeline;

/// <summary>
///     Renders an <see cref="HtmlLayout" /> to PDF using a SkiaSharp drawing pipeline.
///     The renderer owns paint output only and treats layout pages and fragments as read-only inputs.
/// </summary>
internal sealed class PdfRenderer
{
    private readonly IFileSystemReader _fileSystemReader;
    private readonly ISkiaTypefaceFactory _typefaceFactory;

    internal PdfRenderer()
        : this(new FileSystemReader(), new SkiaTypefaceFactory())
    {
    }

    internal PdfRenderer(IFileSystemReader fileSystemReader, ISkiaTypefaceFactory typefaceFactory)
    {
        _fileSystemReader = fileSystemReader ?? throw new ArgumentNullException(nameof(fileSystemReader));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
    }

    public byte[] Render(
        HtmlLayout htmlLayout,
        PdfRenderSettings? settings = null,
        IDiagnosticsSink? diagnosticsSink = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(htmlLayout);
        settings ??= new();
        ValidateSettings(settings);

        return RenderWithSkia(htmlLayout, settings, diagnosticsSink, cancellationToken);
    }

    private static void ValidateSettings(PdfRenderSettings settings)
    {
        if (settings.MaxImageSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PdfRenderSettings.MaxImageSizeBytes),
                settings.MaxImageSizeBytes,
                "PdfRenderSettings.MaxImageSizeBytes must be greater than zero.");
        }

        if (settings.IncludeRawImageSources && settings.MaxRawImageSourceLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PdfRenderSettings.MaxRawImageSourceLength),
                settings.MaxRawImageSourceLength,
                "PdfRenderSettings.MaxRawImageSourceLength must be greater than zero.");
        }
    }

    private byte[] RenderWithSkia(
        HtmlLayout layout,
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        if (document is null)
        {
            throw new InvalidOperationException("Failed to create Skia PDF document.");
        }

        using var fontCache = new SkiaFontCache(_fileSystemReader, _typefaceFactory);
        var paintOrder = new PaintCommandPlanner();
        var drawer = new SkiaPaintCommandDrawer(settings, fontCache, diagnosticsSink);

        foreach (var page in layout.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = page.PageSize;
            ValidatePageSize(page);
            using var canvas = document.BeginPage(size.Width, size.Height)
                ?? throw new InvalidOperationException(
                    $"Failed to create Skia PDF page {page.PageNumber} ({size.Width}x{size.Height}).");

            var commands = paintOrder.Resolve(page);
            drawer.Draw(canvas, commands);
            document.EndPage();
        }

        document.Close();

        return stream.ToArray();
    }

    private static void ValidatePageSize(LayoutPage page)
    {
        var size = page.PageSize;
        if (float.IsFinite(size.Width) &&
            float.IsFinite(size.Height) &&
            size.Width > 0f &&
            size.Height > 0f)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cannot render PDF page {page.PageNumber}: page size must have finite positive width and height.");
    }
}
