using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Test.TestDoubles;

internal sealed class NoopImageMetadataResolver : IImageMetadataResolver
{
    public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            IntrinsicSizePx = new(0, 0)
        };
}