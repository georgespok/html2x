using System.Globalization;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Resolves image layout dimensions from authored, intrinsic, and available sizes.
/// </summary>
internal interface IImageLayoutResolver
{
    ImageLayoutResolution Resolve(ImageBox imageBox, float availableWidth);
}
