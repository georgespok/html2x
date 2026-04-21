using System.Globalization;
using AngleSharp.Dom;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

internal interface IImageLayoutResolver
{
    ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth);
}

internal sealed class ImageLayoutResolver : IImageLayoutResolver
{
    private readonly string _htmlDirectory;
    private readonly IImageProvider _imageProvider;
    private readonly long _maxImageSizeBytes;

    public ImageLayoutResolver(BoxTreeBuildContext? context = null)
    {
        _imageProvider = context?.ImageProvider ?? new NullImageProvider();
        _htmlDirectory = context?.HtmlDirectory ?? Directory.GetCurrentDirectory();
        _maxImageSizeBytes = context?.MaxImageSizeBytes ?? (long)(10 * 1024 * 1024);
    }

    public ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(imageBox);

        var src = imageBox.Element?.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? imageBox.Src;
        var authoredSize = new SizePx(
            ParsePxAttr(imageBox.Element, HtmlCssConstants.HtmlAttributes.Width),
            ParsePxAttr(imageBox.Element, HtmlCssConstants.HtmlAttributes.Height));
        var loadResult = _imageProvider.Load(src, _htmlDirectory, _maxImageSizeBytes);
        var resolvedSizePx = StyleConverter.ResolveImageSize(authoredSize, loadResult.IntrinsicSizePx)
            .ClampMin(0d, 0d);

        var contentSize = new SizePt((float)resolvedSizePx.WidthOrZero, (float)resolvedSizePx.HeightOrZero)
            .Safe()
            .ClampMin(0f, 0f);

        if (availableWidth > 0f && contentSize.Width > availableWidth)
        {
            var scale = availableWidth / contentSize.Width;
            contentSize = contentSize.Scale(scale);
        }

        var padding = imageBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(imageBox.Style.Borders).Safe();
        var totalSize = contentSize
            .Inflate(padding.Horizontal + border.Horizontal, padding.Vertical + border.Vertical)
            .ClampMin(0f, 0f);

        return new ImageLayoutResolution(
            src,
            authoredSize,
            new SizePx(contentSize.Width, contentSize.Height),
            loadResult.Status == ImageLoadStatus.Missing,
            loadResult.Status == ImageLoadStatus.Oversize,
            contentSize.Width,
            contentSize.Height,
            totalSize.Width,
            totalSize.Height);
    }

    private static double? ParsePxAttr(IElement? element, string attributeName)
    {
        var value = element?.GetAttribute(attributeName);
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private sealed class NullImageProvider : IImageProvider
    {
        public ImageLoadResult Load(string src, string baseDirectory, long maxBytes)
        {
            return new ImageLoadResult
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = new SizePx(0d, 0d)
            };
        }
    }
}

internal readonly record struct ImageLayoutResolution(
    string Src,
    SizePx AuthoredSizePx,
    SizePx IntrinsicSizePx,
    bool IsMissing,
    bool IsOversize,
    float ContentWidth,
    float ContentHeight,
    float TotalWidth,
    float TotalHeight);
