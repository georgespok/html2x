using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Contracts.Geometry;

/// <summary>
/// Carries per-build inputs required to compute layout geometry for one document.
/// </summary>
internal sealed class LayoutGeometryRequest
{
    public SizePt PageSize { get; init; } = PaperSizes.Letter;

    public IImageMetadataResolver? ImageMetadataResolver { get; init; }

    public string? ResourceBaseDirectory { get; init; }

    public long MaxImageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public static LayoutGeometryRequest Default { get; } = new();
}
