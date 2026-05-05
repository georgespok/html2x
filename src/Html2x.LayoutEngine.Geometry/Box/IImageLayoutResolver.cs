namespace Html2x.LayoutEngine.Geometry.Box;


/// <summary>
/// Resolves image layout dimensions from authored, intrinsic, and available sizes.
/// </summary>
internal interface IImageLayoutResolver
{
    ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth);
}
