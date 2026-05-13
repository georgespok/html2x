using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Resources;

namespace Html2x.LayoutEngine.Test.TestDoubles;

internal sealed class NoopImageMetadataResolver : IImageMetadataResolver
{
    public ImageMetadataResult Resolve(string src) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            IntrinsicSizePx = new(0, 0)
        };
}
