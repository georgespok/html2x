using System.Globalization;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Resolves image content and border-box dimensions for layout.
/// </summary>
internal sealed class ImageLayoutResolver : IImageLayoutResolver
{
    private readonly string _htmlDirectory;
    private readonly IImageMetadataResolver _imageMetadataResolver;
    private readonly long _maxImageSizeBytes;

    public ImageLayoutResolver(LayoutGeometryRequest? request = null)
    {
        _imageMetadataResolver = request?.ImageMetadataResolver ?? new NullImageMetadataResolver();
        _htmlDirectory = string.IsNullOrWhiteSpace(request?.HtmlDirectory)
            ? AppContext.BaseDirectory
            : request.HtmlDirectory;
        _maxImageSizeBytes = request?.MaxImageSizeBytes ?? (long)(10 * 1024 * 1024);
    }

    public ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(imageBox);

        var src = imageBox.Element?.GetAttribute(HtmlCssConstants.HtmlAttributes.Src) ?? imageBox.Src;
        var htmlAuthoredSize = new SizePx(
            ParsePxAttr(imageBox.Element, HtmlCssConstants.HtmlAttributes.Width),
            ParsePxAttr(imageBox.Element, HtmlCssConstants.HtmlAttributes.Height));
        var authoredSize = ResolveAuthoredMetadataSize(imageBox, htmlAuthoredSize);
        var padding = imageBox.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(imageBox.Style.Borders).Safe();
        var authoredLayoutSize = ResolveAuthoredLayoutSize(imageBox, htmlAuthoredSize);
        var loadResult = _imageMetadataResolver.Resolve(src, _htmlDirectory, _maxImageSizeBytes);
        var loadStatus = ToImageLoadStatus(loadResult.Status);
        var intrinsicLayoutSize = ToLayoutSize(loadResult.IntrinsicSizePx);
        var contentSize = ResolveLayoutSize(authoredLayoutSize, intrinsicLayoutSize)
            .Safe()
            .ClampMin(0f, 0f);

        if (availableWidth > 0f && contentSize.Width > availableWidth)
        {
            var scale = availableWidth / contentSize.Width;
            contentSize = contentSize.Scale(scale);
        }

        var totalSize = contentSize
            .Inflate(padding.Horizontal + border.Horizontal, padding.Vertical + border.Vertical)
            .ClampMin(0f, 0f);

        return new ImageLayoutResolution(
            src,
            authoredSize,
            loadResult.IntrinsicSizePx,
            loadStatus,
            IsMissing(loadStatus),
            loadStatus == ImageLoadStatus.Oversize,
            contentSize.Width,
            contentSize.Height,
            totalSize.Width,
            totalSize.Height);
    }

    private static ImageLoadStatus ToImageLoadStatus(ImageMetadataStatus status) =>
        status switch
        {
            ImageMetadataStatus.Ok => ImageLoadStatus.Ok,
            ImageMetadataStatus.Oversize => ImageLoadStatus.Oversize,
            ImageMetadataStatus.InvalidDataUri => ImageLoadStatus.InvalidDataUri,
            ImageMetadataStatus.DecodeFailed => ImageLoadStatus.DecodeFailed,
            ImageMetadataStatus.OutOfScope => ImageLoadStatus.OutOfScope,
            _ => ImageLoadStatus.Missing
        };

    private static bool IsMissing(ImageLoadStatus status) =>
        status is not ImageLoadStatus.Ok and not ImageLoadStatus.Oversize;

    private static SizePx ResolveAuthoredMetadataSize(ImageBox imageBox, SizePx htmlAuthoredSize)
    {
        return new SizePx(
            imageBox.Style.WidthPt.HasValue
                ? CssUnitConversion.PtToCssPx(imageBox.Style.WidthPt.Value)
                : htmlAuthoredSize.Width,
            imageBox.Style.HeightPt.HasValue
                ? CssUnitConversion.PtToCssPx(imageBox.Style.HeightPt.Value)
                : htmlAuthoredSize.Height);
    }

    private static OptionalSizePt ResolveAuthoredLayoutSize(
        ImageBox imageBox,
        SizePx htmlAuthoredSize)
    {
        var authoredWidth = imageBox.Style.WidthPt
            ?? CssUnitConversion.CssPxToPtOrNull(htmlAuthoredSize.Width);
        var authoredHeight = imageBox.Style.HeightPt
            ?? CssUnitConversion.CssPxToPtOrNull(htmlAuthoredSize.Height);

        return new OptionalSizePt(
            authoredWidth.HasValue
                ? BoxDimensionResolver.ApplyContentWidthConstraints(authoredWidth.Value, imageBox.Style)
                : null,
            authoredHeight.HasValue
                ? BoxDimensionResolver.ResolveContentBoxHeight(imageBox.Style, authoredHeight.Value)
                : null);
    }

    private static OptionalSizePt ToLayoutSize(SizePx size)
    {
        return new OptionalSizePt(
            CssUnitConversion.CssPxToPtOrNull(size.Width),
            CssUnitConversion.CssPxToPtOrNull(size.Height));
    }

    private static SizePt ResolveLayoutSize(OptionalSizePt authored, OptionalSizePt intrinsic)
    {
        var intrinsicWidth = intrinsic.Width is > 0f ? intrinsic.Width : null;
        var intrinsicHeight = intrinsic.Height is > 0f ? intrinsic.Height : null;

        if (authored.HasWidth && authored.HasHeight)
        {
            return new SizePt(authored.Width!.Value, authored.Height!.Value);
        }

        if (authored.HasWidth && intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            var width = authored.Width!.Value;
            return new SizePt(width, width * intrinsicHeight.Value / intrinsicWidth.Value);
        }

        if (authored.HasHeight && intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            var height = authored.Height!.Value;
            return new SizePt(height * intrinsicWidth.Value / intrinsicHeight.Value, height);
        }

        if (intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            return new SizePt(intrinsicWidth.Value, intrinsicHeight.Value);
        }

        if (authored.HasWidth)
        {
            var width = authored.Width!.Value;
            return new SizePt(width, width);
        }

        if (authored.HasHeight)
        {
            var height = authored.Height!.Value;
            return new SizePt(height, height);
        }

        return new SizePt(0f, 0f);
    }

    private static double? ParsePxAttr(StyledElementFacts? element, string attributeName)
    {
        var value = element?.GetAttribute(attributeName);
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) &&
            double.IsFinite(parsed))
        {
            return parsed;
        }

        return null;
    }

    /// <summary>
    /// Provides zero intrinsic image dimensions when no provider is configured.
    /// </summary>
    private sealed class NullImageMetadataResolver : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
        {
            return new ImageMetadataResult
            {
                Src = src,
                Status = ImageMetadataStatus.Ok,
                IntrinsicSizePx = new SizePx(0d, 0d)
            };
        }
    }

    /// <summary>
    /// Carries optional point dimensions during image size resolution.
    /// </summary>
    private readonly record struct OptionalSizePt(float? Width, float? Height)
    {
        public bool HasWidth => Width.HasValue;

        public bool HasHeight => Height.HasValue;
    }
}
