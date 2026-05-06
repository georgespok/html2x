using Html2x.LayoutEngine.Geometry.Box;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Describes a measured inline object placed as one atomic text run.
/// </summary>
internal sealed record InlineObjectLayout(
    BlockBox ContentBox,
    TextLayoutResult Layout,
    float ContentWidth,
    float ContentHeight,
    float BorderBoxWidth,
    float BorderBoxHeight,
    float Baseline,
    ImageLayoutResolution? ImageResolution = null);