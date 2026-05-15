using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Writing;

/// <summary>
///     Writes common block-level layout state after geometry facts have been resolved.
/// </summary>
internal sealed class LayoutBoxStateWriter
{
    public void ApplyBlockLayout(
        BlockBox block,
        BlockMeasurementBasis measurement,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(block);

        ApplyBlockLayout(
            block,
            measurement.Margin,
            measurement.Padding,
            geometry);
    }

    public void ApplyBlockLayout(
        BlockBox block,
        Spacing margin,
        Spacing padding,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(block);

        block.ApplyBlockLayoutState(
            margin,
            padding,
            block.Style.TextAlign,
            geometry);
    }

    public void ApplyInlineLayout(BlockBox block, InlineLayoutResult inlineLayout)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(inlineLayout);

        block.ApplyInlineLayoutState(inlineLayout);
    }

    public void ApplyTextAlignment(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);

        block.ApplyTextAlignmentState(block.Style.TextAlign);
    }
}
