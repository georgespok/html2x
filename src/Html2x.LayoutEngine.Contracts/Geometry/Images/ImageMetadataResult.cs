using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Contracts.Geometry.Images;

internal sealed class ImageMetadataResult
{
    public required string Src { get; init; }

    public ImageMetadataStatus Status { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
