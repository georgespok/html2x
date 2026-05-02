namespace Html2x.LayoutEngine.Geometry.Images;

public interface IImageMetadataResolver
{
    ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes);
}
