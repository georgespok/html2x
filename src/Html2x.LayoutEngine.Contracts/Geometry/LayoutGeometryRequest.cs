using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry.Images;

namespace Html2x.LayoutEngine.Contracts.Geometry;

/// <summary>
/// Carries runtime inputs required to compute layout geometry for one document.
/// </summary>
internal sealed class LayoutGeometryRequest
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    public IImageMetadataResolver? ImageMetadataResolver { get; init; }

    public string? HtmlDirectory { get; init; }

    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public static LayoutGeometryRequest Default { get; } = new();
}
