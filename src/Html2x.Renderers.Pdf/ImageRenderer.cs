using System;
using System.IO;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Options;
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

    public ImageRenderer(PdfOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _htmlDirectory = string.IsNullOrWhiteSpace(options.HtmlDirectory)
            ? Directory.GetCurrentDirectory()
            : options.HtmlDirectory;
    }

    public void Render(IContainer container, ImageFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(fragment);

        var width = (float)fragment.Rect.Width;
        var height = (float)fragment.Rect.Height;

        if (fragment.IsMissing || fragment.IsOversize || width <= 0 || height <= 0)
        {
            RenderPlaceholder(container, width, height);
            return;
        }

        var imgBytes = ImageLoader.Load(fragment.Src, _htmlDirectory);
        if (imgBytes is null)
        {
            RenderPlaceholder(container, width, height);
            return;
        }

        if (imgBytes.LongLength > _options.MaxImageSizeBytes)
        {
            RenderPlaceholder(container, width, height);
            return;
        }

        container.Element(element =>
        {
            element.Width(width)
                   .Height(height)
                   .Image(imgBytes);
        });
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
}
