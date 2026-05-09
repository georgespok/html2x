using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.Resources;

namespace Html2x;

/// <summary>
///     Adapts shared image resource loading to the layout image metadata seam.
/// </summary>
internal sealed class ImageResourceMetadataResolver : IImageMetadataResolver
{
    private readonly ConversionImageResourceStore _resources;

    public ImageResourceMetadataResolver(ConversionImageResourceStore resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
    {
        var resource = _resources.LoadMetadata(src);
        return new()
        {
            Src = resource.Src,
            Status = resource.Status,
            IntrinsicSizePx = resource.IntrinsicSizePx
        };
    }
}
