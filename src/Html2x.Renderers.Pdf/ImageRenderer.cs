using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.Resources;
using SkiaSharp;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Renders image fragments onto a Skia canvas while honoring size caps and placeholders.
/// </summary>
internal sealed class ImageRenderer
{
    private readonly string _htmlDirectory;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly long _maxImageSizeBytes;
    private const string ImageRenderEvent = "image/render";

    public ImageRenderer(
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _htmlDirectory = ImageResourceLoader.ResolveBaseDirectory(settings.HtmlDirectory);
        _maxImageSizeBytes = settings.MaxImageSizeBytes;
        _diagnosticsSink = diagnosticsSink;
    }

    public void Render(SKCanvas canvas, ImagePaintCommand command)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(command);

        var rect = command.ContentRect;
        var width = rect.Width;
        var height = rect.Height;
        var status = ToRenderStatus(command);

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

        var resource = ImageResourceLoader.Load(command.Src, _htmlDirectory, _maxImageSizeBytes);
        status = ToRenderStatus(resource.Status);
        if (resource.Bytes is null || status != ImageRenderStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        DrawImage(canvas, rect, resource.Bytes);

        Record(command, status, width, height);
    }

    private static void DrawImage(SKCanvas canvas, RectPt rect, byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null)
        {
            RenderPlaceholder(canvas, rect);
            return;
        }

        using var image = SKImage.FromBitmap(bitmap);
        var dest = SkiaGeometryMapper.ToSKRect(rect);
        canvas.DrawImage(image, dest);
    }

    private static void RenderPlaceholder(SKCanvas canvas, RectPt rect)
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

        canvas.DrawRect(SkiaGeometryMapper.ToSKRect(rect), paint);
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

    private static ImageRenderStatus ToRenderStatus(ImageResourceStatus status) =>
        status switch
        {
            ImageResourceStatus.Ok => ImageRenderStatus.Ok,
            ImageResourceStatus.Oversize => ImageRenderStatus.Oversize,
            ImageResourceStatus.InvalidDataUri => ImageRenderStatus.InvalidDataUri,
            ImageResourceStatus.DecodeFailed => ImageRenderStatus.DecodeFailed,
            ImageResourceStatus.OutOfScope => ImageRenderStatus.OutOfScope,
            _ => ImageRenderStatus.Missing
        };

    private static ImageRenderStatus ToRenderStatus(ImagePaintCommand command)
    {
        if (command.Status != ImageLoadStatus.Ok)
        {
            return ToRenderStatus(command.Status);
        }

        return command.IsOversize
            ? ImageRenderStatus.Oversize
            : command.IsMissing ? ImageRenderStatus.Missing : ImageRenderStatus.Ok;
    }

    private static ImageRenderStatus ToRenderStatus(ImageLoadStatus status) =>
        status switch
        {
            ImageLoadStatus.Ok => ImageRenderStatus.Ok,
            ImageLoadStatus.Oversize => ImageRenderStatus.Oversize,
            ImageLoadStatus.InvalidDataUri => ImageRenderStatus.InvalidDataUri,
            ImageLoadStatus.DecodeFailed => ImageRenderStatus.DecodeFailed,
            ImageLoadStatus.OutOfScope => ImageRenderStatus.OutOfScope,
            _ => ImageRenderStatus.Missing
        };

    private enum ImageRenderStatus
    {
        Ok,
        Missing,
        Oversize,
        InvalidDataUri,
        DecodeFailed,
        OutOfScope
    }
}
