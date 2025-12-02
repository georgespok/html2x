using System;
using System.IO;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Renders image fragments into QuestPDF containers while honoring size caps and placeholders.
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

    public void Render(IContainer container, ImageFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(fragment);

        var width = (float)fragment.Rect.Width;
        var height = (float)fragment.Rect.Height;
        var status = fragment.IsMissing
            ? ImageStatus.Missing
            : fragment.IsOversize ? ImageStatus.Oversize : ImageStatus.Ok;

        if (width <= 0 || height <= 0)
        {
            RenderPlaceholder(container, width, height);
            Record(fragment, status, width, height);
            return;
        }

        if (status != ImageStatus.Ok)
        {
            RenderPlaceholder(container, width, height);
            Record(fragment, status, width, height);
            return;
        }

        var imgBytes = ImageLoader.Load(fragment.Src, _htmlDirectory);
        if (imgBytes is null)
        {
            RenderPlaceholder(container, width, height);
            Record(fragment, status, width, height);
            return;
        }

        container.Element(element =>
        {
            element.Width(width)
                   .Height(height)
                   .Image(imgBytes);
        });

        Record(fragment, status, width, height);
    }

    private static void RenderPlaceholder(IContainer container, float width, float height)
    {
        var sized = container;

        if (width > 0)
        {
            sized = sized.Width(width);
        }

        if (height > 0)
        {
            sized = sized.Height(height);
        }

        sized.Element(_ => { });
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
                Status = status
            }
        });
    }
}
