namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Resolves image layout dimensions from authored, intrinsic, and available sizes.
/// </summary>
internal interface IImageSizingRules
{
    ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth);
}