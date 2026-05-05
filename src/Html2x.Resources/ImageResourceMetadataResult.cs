using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.Resources;

internal sealed class ImageResourceMetadataResult
{
    public required string Src { get; init; }

    public ImageLoadStatus Status { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
