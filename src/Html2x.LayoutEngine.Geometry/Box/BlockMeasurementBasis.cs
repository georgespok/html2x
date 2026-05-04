using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Carries resolved block spacing and width values for one layout pass.
/// </summary>
internal readonly record struct BlockMeasurementBasis(
    Spacing Margin,
    Spacing Padding,
    Spacing Border,
    float BorderBoxWidth,
    float ContentFlowWidth);
