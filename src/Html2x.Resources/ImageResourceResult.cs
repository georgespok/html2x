using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Resources;

namespace Html2x.Resources;

internal sealed class ImageResourceResult
{
    public required string Src { get; init; }

    public ImageLoadStatus Status { get; init; }

    public byte[]? Bytes { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
