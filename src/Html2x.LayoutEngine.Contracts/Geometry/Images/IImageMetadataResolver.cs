namespace Html2x.LayoutEngine.Contracts.Geometry.Images;

internal interface IImageMetadataResolver
{
    ImageMetadataResult Resolve(string src);
}
