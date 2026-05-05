using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;


/// <summary>
/// Carries resolved block spacing and width values for one layout pass.
/// </summary>
internal readonly record struct BlockMeasurementBasis(
    Spacing Margin,
    Spacing Padding,
    Spacing Border,
    float BorderBoxWidth,
    float ContentFlowWidth);
