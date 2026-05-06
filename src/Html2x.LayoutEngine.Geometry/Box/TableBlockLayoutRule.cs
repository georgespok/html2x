namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Lays out table blocks through the table grid and placement modules.
/// </summary>
internal sealed class TableBlockLayoutRule(
    TableBlockLayout tableBlockLayout,
    Func<BlockBox, float, float, float, float, float> layoutChildBlocks) : IBlockLayoutRule
{
    private readonly Func<BlockBox, float, float, float, float, float> _layoutChildBlocks =
        layoutChildBlocks ?? throw new ArgumentNullException(nameof(layoutChildBlocks));

    private readonly TableBlockLayout _tableBlockLayout =
        tableBlockLayout ?? throw new ArgumentNullException(nameof(tableBlockLayout));

    public bool CanLayout(BlockBox block) => block is TableBox;

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var table = (TableBox)block;
        _tableBlockLayout.Layout(table, request, _layoutChildBlocks);
        return BlockLayoutRuleResult.ForResolvedBlock(table);
    }
}