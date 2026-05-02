using System.Drawing;
using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Renders image fragments onto a Skia canvas while honoring size caps and placeholders.
/// </summary>
internal sealed class ImageRenderer
{
    private readonly string _htmlDirectory;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private const string ImageRenderEvent = "image/render";

    public ImageRenderer(
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _htmlDirectory = string.IsNullOrWhiteSpace(settings.HtmlDirectory)
            ? Directory.GetCurrentDirectory()
            : settings.HtmlDirectory;
        _diagnosticsSink = diagnosticsSink;
    }

    public void Render(SKCanvas canvas, ImagePaintCommand command)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(command);

        var rect = command.ContentRect;
        var width = rect.Width;
        var height = rect.Height;
        var status = command.IsMissing
            ? ImageRenderStatus.Missing
            : command.IsOversize ? ImageRenderStatus.Oversize : ImageRenderStatus.Ok;

        if (width <= 0 || height <= 0)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        if (status != ImageRenderStatus.Ok)
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

    private void Record(ImagePaintCommand command, ImageRenderStatus status, float width, float height)
    {
        var severity = status == ImageRenderStatus.Ok
            ? DiagnosticSeverity.Info
            : DiagnosticSeverity.Warning;
        var context = new DiagnosticContext(
            Selector: null,
            ElementIdentity: "img",
            StyleDeclaration: null,
            StructuralPath: $"image:{command.Src}",
            RawUserInput: command.Src);

        _diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/render",
            Name: ImageRenderEvent,
            Severity: severity,
            Message: status == ImageRenderStatus.Ok ? null : $"Image render status: {status}.",
            Context: context,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("src", command.Src),
                DiagnosticFields.Field("status", DiagnosticValue.FromEnum(status)),
                DiagnosticFields.Field("renderedWidth", width),
                DiagnosticFields.Field("renderedHeight", height),
                DiagnosticFields.Field("borders", MapBorders(command.Style.Borders))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static DiagnosticObject MapBorders(BorderEdges? borders)
    {
        if (borders is null || !borders.HasAny)
        {
            return DiagnosticObject.Empty;
        }

        return DiagnosticObject.Create(
            DiagnosticObject.Field("top", MapBorderSide(borders.Top)),
            DiagnosticObject.Field("right", MapBorderSide(borders.Right)),
            DiagnosticObject.Field("bottom", MapBorderSide(borders.Bottom)),
            DiagnosticObject.Field("left", MapBorderSide(borders.Left)));
    }

    private static DiagnosticObject? MapBorderSide(BorderSide? side)
    {
        return side is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field("width", side.Width),
                DiagnosticObject.Field("color", side.Color.ToHex()),
                DiagnosticObject.Field("lineStyle", DiagnosticValue.FromEnum(side.LineStyle)));
    }

    private enum ImageRenderStatus
    {
        Ok,
        Missing,
        Oversize
    }
}
