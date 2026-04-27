using System.Drawing;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Renders image fragments onto a Skia canvas while honoring size caps and placeholders.
/// </summary>
internal sealed class ImageRenderer
{
    private readonly PdfOptions _options;
    private readonly string _htmlDirectory;
    private readonly DiagnosticsSession? _diagnostics;
    private const string ImageRenderEvent = "image/render";

    public ImageRenderer(PdfOptions options, DiagnosticsSession? diagnosticsSession)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _htmlDirectory = string.IsNullOrWhiteSpace(options.HtmlDirectory)
            ? Directory.GetCurrentDirectory()
            : options.HtmlDirectory;
        _diagnostics = diagnosticsSession;
    }

    public void Render(SKCanvas canvas, ImagePaintCommand command)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(command);

        var rect = command.ContentRect;
        var width = rect.Width;
        var height = rect.Height;
        var status = command.IsMissing
            ? ImageStatus.Missing
            : command.IsOversize ? ImageStatus.Oversize : ImageStatus.Ok;

        if (width <= 0 || height <= 0)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        if (status != ImageStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        var imgBytes = ImageLoader.Load(command.Src, _htmlDirectory);
        if (imgBytes is null)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        DrawImage(canvas, rect, imgBytes);

        Record(command, status, width, height);
    }

    private static void DrawImage(SKCanvas canvas, RectangleF rect, byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null)
        {
            RenderPlaceholder(canvas, rect);
            return;
        }

        using var image = SKImage.FromBitmap(bitmap);
        var dest = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        canvas.DrawImage(image, dest);
    }

    private static void RenderPlaceholder(SKCanvas canvas, RectangleF rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(220, 220, 220, 255),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        canvas.DrawRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom), paint);
    }

    private void Record(ImagePaintCommand command, ImageStatus status, float width, float height)
    {
        if (_diagnostics is null)
        {
            return;
        }

        var severity = status == ImageStatus.Ok
            ? DiagnosticSeverity.Info
            : DiagnosticSeverity.Warning;
        var context = new DiagnosticContext(
            Selector: null,
            ElementIdentity: "img",
            StyleDeclaration: null,
            StructuralPath: $"image:{command.Src}",
            RawUserInput: command.Src);

        _diagnostics.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = ImageRenderEvent,
            Timestamp = DateTimeOffset.UtcNow,
            Severity = severity,
            Context = context,
            RawUserInput = command.Src,
            Payload = new ImageRenderPayload
            {
                Src = command.Src,
                Severity = severity,
                Context = context,
                RenderedSize = new Abstractions.Measurements.Units.SizePt(width, height),
                Status = status,
                Borders = command.Style.Borders
            }
        });
    }
}
