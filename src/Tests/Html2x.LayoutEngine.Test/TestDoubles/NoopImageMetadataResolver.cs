using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Test.TestDoubles;

internal sealed class NoopImageMetadataResolver : IImageMetadataResolver
{
    public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes) =>
        new()
        {
            Src = src,
            Status = ImageMetadataStatus.Ok,
            IntrinsicSizePx = new SizePx(0, 0)
        };
}
