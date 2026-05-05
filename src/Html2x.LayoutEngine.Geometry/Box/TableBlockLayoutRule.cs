namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Lays out table blocks through the table grid and placement modules.
/// </summary>
internal sealed class TableBlockLayoutRule(
    TableBlockLayoutApplier tableBlockLayoutApplier,
    Func<BlockBox, float, float, float, float, float> layoutChildBlocks) : IBlockLayoutRule
{
    private readonly TableBlockLayoutApplier _tableBlockLayoutApplier =
        tableBlockLayoutApplier ?? throw new ArgumentNullException(nameof(tableBlockLayoutApplier));
    private readonly Func<BlockBox, float, float, float, float, float> _layoutChildBlocks =
        layoutChildBlocks ?? throw new ArgumentNullException(nameof(layoutChildBlocks));

    public bool CanLayout(BlockBox block)
    {
        return block is TableBox;
    }

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var table = (TableBox)block;
        _tableBlockLayoutApplier.Apply(table, request, _layoutChildBlocks);
        return BlockLayoutRuleResult.ForResolvedBlock(table);
    }
}
