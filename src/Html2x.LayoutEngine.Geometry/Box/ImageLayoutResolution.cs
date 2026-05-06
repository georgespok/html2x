using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Carries resolved image dimensions and load status for layout consumers.
/// </summary>
internal readonly record struct ImageLayoutResolution(
    string Src,
    SizePx AuthoredSizePx,
    SizePx IntrinsicSizePx,
    ImageLoadStatus Status,
    float ContentWidth,
    float ContentHeight,
    float TotalWidth,
    float TotalHeight);