using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Images;

public sealed class ImageLoadResult
{
    public required string Src { get; init; }

    public ImageLoadStatus Status { get; init; }

    public SizePx IntrinsicSizePx { get; init; }
}
