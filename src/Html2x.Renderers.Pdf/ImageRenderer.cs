using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Html2x.Resources;
using SkiaSharp;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Renders image fragments onto a Skia canvas while honoring size caps and placeholders.
/// </summary>
internal sealed class ImageRenderer
{
    private readonly string _resourceBaseDirectory;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly long _maxImageSizeBytes;

    public ImageRenderer(
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _resourceBaseDirectory = ImageResourceLoader.ResolveBaseDirectory(settings.ResourceBaseDirectory);
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
        var status = command.Status;

        if (width <= 0 || height <= 0)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        if (status != ImageLoadStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            Record(command, status, width, height);
            return;
        }

        var resource = ImageResourceLoader.Load(command.Src, _resourceBaseDirectory, _maxImageSizeBytes);
        status = resource.Status;
        if (resource.Bytes is null || status != ImageLoadStatus.Ok)
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
        var dest = SkiaGeometryMapper.ToSkRect(rect);
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

        canvas.DrawRect(SkiaGeometryMapper.ToSkRect(rect), paint);
    }

    private void Record(ImagePaintCommand command, ImageLoadStatus status, float width, float height)
    {
        var severity = status == ImageLoadStatus.Ok
            ? DiagnosticSeverity.Info
            : DiagnosticSeverity.Warning;
        var context = new DiagnosticContext(
            Selector: null,
            ElementIdentity: ImageRenderDiagnosticNames.Context.ImageElement,
            StyleDeclaration: null,
            StructuralPath: $"image:{command.Src}",
            RawUserInput: command.Src);

        _diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: ImageRenderDiagnosticNames.Stages.Render,
            Name: ImageRenderDiagnosticNames.Events.Render,
            Severity: severity,
            Message: status == ImageLoadStatus.Ok ? null : $"Image render status: {status}.",
            Context: context,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Src, command.Src),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Status, DiagnosticValue.FromEnum(status)),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.RenderedWidth, width),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.RenderedHeight, height),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Borders, MapBorders(command.Style.Borders))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static DiagnosticObject MapBorders(BorderEdges? borders)
    {
        if (borders is null || !borders.HasAny)
        {
            return DiagnosticObject.Empty;
        }

        return DiagnosticObject.Create(
            DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Top, MapBorderSide(borders.Top)),
            DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Right, MapBorderSide(borders.Right)),
            DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Bottom, MapBorderSide(borders.Bottom)),
            DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Left, MapBorderSide(borders.Left)));
    }

    private static DiagnosticObject? MapBorderSide(BorderSide? side)
    {
        return side is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Width, side.Width),
                DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Color, side.Color.ToHex()),
                DiagnosticObject.Field(
                    ImageRenderDiagnosticNames.Fields.LineStyle,
                    DiagnosticValue.FromEnum(side.LineStyle)));
    }

}
