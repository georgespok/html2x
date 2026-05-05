using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Contracts.Geometry.Images;

internal sealed class ImageMetadataResult
{
    public required string Src { get; init; }

    public ImageLoadStatus Status { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
