using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.Resources;

namespace Html2x;

/// <summary>
/// Adapts shared image resource loading to the layout image metadata seam.
/// </summary>
internal sealed class FileImageProvider : IImageMetadataResolver
{
    public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
    {
        var resource = ImageResourceLoader.Load(src, baseDirectory, maxBytes);
        return new ImageMetadataResult
        {
            Src = resource.Src,
            Status = ToMetadataStatus(resource.Status),
            IntrinsicSizePx = resource.IntrinsicSizePx
        };
    }

    private static ImageMetadataStatus ToMetadataStatus(ImageResourceStatus status) =>
        status switch
        {
            ImageResourceStatus.Ok => ImageMetadataStatus.Ok,
            ImageResourceStatus.Oversize => ImageMetadataStatus.Oversize,
            ImageResourceStatus.InvalidDataUri => ImageMetadataStatus.InvalidDataUri,
            ImageResourceStatus.DecodeFailed => ImageMetadataStatus.DecodeFailed,
            ImageResourceStatus.OutOfScope => ImageMetadataStatus.OutOfScope,
            _ => ImageMetadataStatus.Missing
        };
}
