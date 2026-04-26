using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Formatting;

/// <summary>
/// Runs the current block formatting context and owns normal block-flow geometry.
/// </summary>
internal interface IBlockFormattingContextRunner
{
    BlockBox LayoutBlock(BlockBox block, BlockLayoutRequest request);
}

/// <summary>
/// Runs the current inline formatting context and owns inline line placement.
/// </summary>
internal interface IInlineFormattingContextRunner
{
    InlineLayoutResult LayoutInlineContent(BlockBox block, InlineLayoutRequest request);

    InlineLayoutResult MeasureInlineContent(BlockBox block, InlineLayoutRequest request);
}

/// <summary>
/// Runs the current table formatting context and owns row and cell measurement.
/// </summary>
internal interface ITableFormattingContextRunner
{
    TableLayoutResult LayoutTable(TableBox table, float availableWidth);
}

/// <summary>
/// Runs current inline-block measurement without adding new inline-block behavior.
/// </summary>
internal interface IInlineBlockFormattingContextRunner
{
    bool TryLayoutInlineBlock(InlineBox inline, float availableWidth, out InlineObjectLayout layout);
}
