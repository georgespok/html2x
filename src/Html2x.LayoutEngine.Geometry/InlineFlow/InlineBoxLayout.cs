using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Images;

namespace Html2x.LayoutEngine.Geometry.InlineFlow;

/// <summary>
///     Describes a measured inline box placed as one atomic text run.
/// </summary>
internal sealed record InlineBoxLayout(
    BlockBox ContentBox,
    TextLayoutResult Layout,
    float ContentWidth,
    float ContentHeight,
    float BorderBoxWidth,
    float BorderBoxHeight,
    float Baseline,
    ImageLayoutResolution? ImageResolution = null);
