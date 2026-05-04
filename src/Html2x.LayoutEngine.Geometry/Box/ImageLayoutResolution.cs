using System.Globalization;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Carries resolved image dimensions and load status for layout consumers.
/// </summary>
internal readonly record struct ImageLayoutResolution(
    string Src,
    SizePx AuthoredSizePx,
    SizePx IntrinsicSizePx,
    ImageLoadStatus Status,
    bool IsMissing,
    bool IsOversize,
    float ContentWidth,
    float ContentHeight,
    float TotalWidth,
    float TotalHeight);
