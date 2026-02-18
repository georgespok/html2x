using Html2x.Abstractions.Images;

namespace Html2x.LayoutEngine.Test.TestDoubles;

internal sealed class NoopImageProvider : IImageProvider
{
    public ImageLoadResult Load(string src, string baseDirectory, long maxBytes) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            IntrinsicSizePx = new Abstractions.Measurements.Units.SizePx(0, 0)
        };
}
