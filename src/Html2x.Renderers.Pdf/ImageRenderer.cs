using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Drawing;
using Html2x.Renderers.Pdf.Paint;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;
using Html2x.Resources;
using SkiaSharp;
using Html2x.RenderModel.Resources;

namespace Html2x.Renderers.Pdf;

/// <summary>
///     Renders image fragments onto a Skia canvas while honoring size caps and placeholders.
/// </summary>
internal sealed class ImageRenderer
{
    private const int MaxDiagnosticDisplaySourceLength = 256;

    private readonly bool _includeRawImageSources;
    private readonly IDiagnosticsSink? _diagnosticsSink;
    private readonly IImageResourceReader? _imageResources;
    private readonly long _maxImageSizeBytes;
    private readonly int _maxRawImageSourceLength;
    private readonly string _resourceBaseDirectory;

    public ImageRenderer(
        PdfRenderSettings settings,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _resourceBaseDirectory = ImageResourceLoader.ResolveBaseDirectory(settings.ResourceBaseDirectory);
        _maxImageSizeBytes = settings.MaxImageSizeBytes;
        _includeRawImageSources = settings.IncludeRawImageSources;
        _maxRawImageSourceLength = settings.MaxRawImageSourceLength;
        _imageResources = settings.ImageResources;
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
            EmitRenderDiagnostic(command, status, width, height);
            return;
        }

        if (status != ImageLoadStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            EmitRenderDiagnostic(command, status, width, height);
            return;
        }

        var resource = _imageResources?.Load(command.Src)
                       ?? ImageResourceLoader.Load(command.Src, _resourceBaseDirectory, _maxImageSizeBytes);
        status = resource.Status;
        if (resource.Bytes is null || status != ImageLoadStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            EmitRenderDiagnostic(command, status, width, height);
            return;
        }

        if (!DrawImage(canvas, rect, resource.Bytes))
        {
            status = ImageLoadStatus.DecodeFailed;
        }

        EmitRenderDiagnostic(command, status, width, height);
    }

    private static bool DrawImage(SKCanvas canvas, RectPt rect, byte[] bytes)
    {
        using var bitmap = TryDecodeBitmap(bytes);
        if (bitmap is null)
        {
            RenderPlaceholder(canvas, rect);
            return false;
        }

        using var image = SKImage.FromBitmap(bitmap);
        var dest = SkiaGeometryAdapter.ToSkRect(rect);
        canvas.DrawImage(image, dest);
        return true;
    }

    private static SKBitmap? TryDecodeBitmap(byte[] bytes)
    {
        try
        {
            return SKBitmap.Decode(bytes);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void RenderPlaceholder(SKCanvas canvas, RectPt rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new(220, 220, 220, 255),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        canvas.DrawRect(SkiaGeometryAdapter.ToSkRect(rect), paint);
    }

    private void EmitRenderDiagnostic(ImagePaintCommand command, ImageLoadStatus status, float width, float height)
    {
        var severity = status == ImageLoadStatus.Ok
            ? DiagnosticSeverity.Info
            : DiagnosticSeverity.Warning;
        var diagnosticSource = CreateDiagnosticSource(command.Src);
        var context = new DiagnosticContext(
            null,
            ImageRenderDiagnosticNames.ContextValues.ImageElement,
            null,
            $"image:{diagnosticSource}",
            _includeRawImageSources
                ? TruncateRaw(command.Src, _maxRawImageSourceLength)
                : null);

        _diagnosticsSink?.Emit(new(
            ImageRenderDiagnosticNames.Stages.Render,
            ImageRenderDiagnosticNames.Events.Render,
            severity,
            status == ImageLoadStatus.Ok ? null : $"Image render status: {status}.",
            context,
            DiagnosticFields.Create(
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Src, diagnosticSource),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Status, DiagnosticValue.FromEnum(status)),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.RenderedWidth, width),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.RenderedHeight, height),
                DiagnosticFields.Field(ImageRenderDiagnosticNames.Fields.Borders, MapBorders(command.Style.Borders)))));
    }

    private static string CreateDiagnosticSource(string src)
    {
        if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return CreateDataUriDiagnosticSource(src);
        }

        if (IsSensitivePath(src))
        {
            var fileName = GetPathDisplayFileName(src);
            return string.IsNullOrWhiteSpace(fileName)
                ? "[path]"
                : TruncateDisplay($"[path]/{fileName}", MaxDiagnosticDisplaySourceLength);
        }

        return TruncateDisplay(src, MaxDiagnosticDisplaySourceLength);
    }

    private static string CreateDataUriDiagnosticSource(string src)
    {
        var commaIndex = src.IndexOf(',', StringComparison.Ordinal);
        var metadata = commaIndex < 0
            ? "data:"
            : src[..commaIndex];

        return TruncateDisplay($"{metadata},[omitted]", MaxDiagnosticDisplaySourceLength);
    }

    private static bool IsSensitivePath(string src) =>
        Path.IsPathRooted(src) ||
        HasWindowsDriveRoot(src) ||
        HasParentPathSegment(src);

    private static bool HasWindowsDriveRoot(string src) =>
        src.Length >= 3 &&
        char.IsAsciiLetter(src[0]) &&
        src[1] == ':' &&
        src[2] is '\\' or '/';

    private static bool HasParentPathSegment(string src)
    {
        var segments = src.Split(
            ['/', '\\'],
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("..", StringComparer.Ordinal);
    }

    private static string GetPathDisplayFileName(string src)
    {
        var normalized = src.Replace('\\', '/');
        var separatorIndex = normalized.LastIndexOf('/');
        return separatorIndex < 0
            ? normalized
            : normalized[(separatorIndex + 1)..];
    }

    private static string TruncateDisplay(string value, int maxLength)
    {
        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        const string marker = "...";
        return maxLength <= marker.Length
            ? value[..maxLength]
            : string.Concat(value.AsSpan(0, maxLength - marker.Length), marker);
    }

    private static string TruncateRaw(string value, int maxLength) =>
        maxLength <= 0 || value.Length <= maxLength
            ? value
            : value[..maxLength];

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

    private static DiagnosticObject? MapBorderSide(BorderSide? side) =>
        side is null
            ? null
            : DiagnosticObject.Create(
                DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Width, side.Width),
                DiagnosticObject.Field(ImageRenderDiagnosticNames.Fields.Color, side.Color.ToHex()),
                DiagnosticObject.Field(
                    ImageRenderDiagnosticNames.Fields.LineStyle,
                    DiagnosticValue.FromEnum(side.LineStyle)));
}
