using System.Globalization;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Resolves image content and border-box dimensions for layout.
/// </summary>
internal sealed class ImageSizingRules(LayoutGeometryRequest? request = null) : IImageSizingRules
{
    private readonly IImageMetadataResolver _imageMetadataResolver =
        request?.ImageMetadataResolver ?? new NullImageMetadataResolver();

    private readonly long _maxImageSizeBytes = request?.MaxImageSizeBytes ?? 10 * 1024 * 1024;

    private readonly string _resourceBaseDirectory = string.IsNullOrWhiteSpace(request?.ResourceBaseDirectory)
        ? AppContext.BaseDirectory
        : request.ResourceBaseDirectory;

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
        var loadResult = _imageMetadataResolver.Resolve(src, _resourceBaseDirectory, _maxImageSizeBytes);
        var loadStatus = loadResult.Status;
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

        return new(
            src,
            authoredSize,
            loadResult.IntrinsicSizePx,
            loadStatus,
            contentSize.Width,
            contentSize.Height,
            totalSize.Width,
            totalSize.Height);
    }

    private static SizePx ResolveAuthoredMetadataSize(ImageBox imageBox, SizePx htmlAuthoredSize) =>
        new(
            imageBox.Style.WidthPt.HasValue
                ? CssUnitConversion.PtToCssPx(imageBox.Style.WidthPt.Value)
                : htmlAuthoredSize.Width,
            imageBox.Style.HeightPt.HasValue
                ? CssUnitConversion.PtToCssPx(imageBox.Style.HeightPt.Value)
                : htmlAuthoredSize.Height);

    private static OptionalSizePt ResolveAuthoredLayoutSize(
        ImageBox imageBox,
        SizePx htmlAuthoredSize)
    {
        var authoredWidth = imageBox.Style.WidthPt
                            ?? CssUnitConversion.CssPxToPtOrNull(htmlAuthoredSize.Width);
        var authoredHeight = imageBox.Style.HeightPt
                             ?? CssUnitConversion.CssPxToPtOrNull(htmlAuthoredSize.Height);

        return new(
            authoredWidth.HasValue
                ? BoxDimensionRules.ApplyContentWidthConstraints(authoredWidth.Value, imageBox.Style)
                : null,
            authoredHeight.HasValue
                ? BoxDimensionRules.ResolveContentBoxHeight(imageBox.Style, authoredHeight.Value)
                : null);
    }

    private static OptionalSizePt ToLayoutSize(SizePx size) =>
        new(
            CssUnitConversion.CssPxToPtOrNull(size.Width),
            CssUnitConversion.CssPxToPtOrNull(size.Height));

    private static SizePt ResolveLayoutSize(OptionalSizePt authored, OptionalSizePt intrinsic)
    {
        var intrinsicWidth = intrinsic.Width is > 0f ? intrinsic.Width : null;
        var intrinsicHeight = intrinsic.Height is > 0f ? intrinsic.Height : null;

        if (authored.HasWidth && authored.HasHeight)
        {
            return new(authored.Width!.Value, authored.Height!.Value);
        }

        if (authored.HasWidth && intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            var width = authored.Width!.Value;
            return new(width, width * intrinsicHeight.Value / intrinsicWidth.Value);
        }

        if (authored.HasHeight && intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            var height = authored.Height!.Value;
            return new(height * intrinsicWidth.Value / intrinsicHeight.Value, height);
        }

        if (intrinsicWidth.HasValue && intrinsicHeight.HasValue)
        {
            return new(intrinsicWidth.Value, intrinsicHeight.Value);
        }

        if (authored.HasWidth)
        {
            var width = authored.Width!.Value;
            return new(width, width);
        }

        if (authored.HasHeight)
        {
            var height = authored.Height!.Value;
            return new(height, height);
        }

        return new(0f, 0f);
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
    ///     Provides zero intrinsic image dimensions when no provider is configured.
    /// </summary>
    private sealed class NullImageMetadataResolver : IImageMetadataResolver
    {
        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes) =>
            new()
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = new(0d, 0d)
            };
    }

    /// <summary>
    ///     Carries optional point dimensions during image size resolution.
    /// </summary>
    private readonly record struct OptionalSizePt(float? Width, float? Height)
    {
        public bool HasWidth => Width.HasValue;

        public bool HasHeight => Height.HasValue;
    }
}