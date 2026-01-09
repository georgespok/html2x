using System;
using System.IO;
using System.Drawing;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
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

    public ImageRenderer(PdfOptions options, DiagnosticsSession? diagnosticsSession)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _htmlDirectory = string.IsNullOrWhiteSpace(options.HtmlDirectory)
            ? Directory.GetCurrentDirectory()
            : options.HtmlDirectory;
        _diagnostics = diagnosticsSession;
    }

    public void Render(SKCanvas canvas, ImageFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(fragment);

        var rect = fragment.ContentRect == default
            ? fragment.Rect
            : fragment.ContentRect;
        var width = (float)rect.Width;
        var height = (float)rect.Height;
        var status = fragment.IsMissing
            ? ImageStatus.Missing
            : fragment.IsOversize ? ImageStatus.Oversize : ImageStatus.Ok;

        if (width <= 0 || height <= 0)
        {
            RenderPlaceholder(canvas, rect);
            Record(fragment, status, width, height);
            return;
        }

        if (status != ImageStatus.Ok)
        {
            RenderPlaceholder(canvas, rect);
            Record(fragment, status, width, height);
            return;
        }

        var imgBytes = ImageLoader.Load(fragment.Src, _htmlDirectory);
        if (imgBytes is null)
        {
            RenderPlaceholder(canvas, rect);
            Record(fragment, status, width, height);
            return;
        }

        DrawImage(canvas, rect, imgBytes);

        Record(fragment, status, width, height);
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

    private void Record(ImageFragment fragment, ImageStatus status, float width, float height)
    {
        if (_diagnostics is null)
        {
            return;
        }

        _diagnostics.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "ImageRender",
            Timestamp = DateTimeOffset.UtcNow,
            Payload = new ImageRenderPayload
            {
                Src = fragment.Src,
                RenderedWidth = width,
                RenderedHeight = height,
                Status = status,
                Borders = fragment.Style?.Borders
            }
        });
    }
}
