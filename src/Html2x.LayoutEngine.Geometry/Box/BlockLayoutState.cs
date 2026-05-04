using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Applies mutable layout state to internal boxes in one place.
/// </summary>
internal static class BlockLayoutState
{
    public static void Apply(BlockBox node, BlockMeasurementBasis measurement, UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(node);

        node.Margin = measurement.Margin;
        node.Padding = measurement.Padding;
        node.TextAlign = node.Style.TextAlign ?? HtmlCssConstants.Defaults.TextAlign;
        node.ApplyLayoutGeometry(geometry);
    }
}
