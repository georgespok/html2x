namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal sealed record BlockLayoutRuleResult(
    BlockBox Block,
    InlineLayoutResult? InlineLayout,
    IReadOnlyList<BlockLayoutRuleResult> Children,
    IReadOnlyList<BlockFlowItemLayout>? Flow)
{
    public static BlockLayoutRuleResult ForResolvedBlock(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new(block, null, [], null);
    }

    public static BlockLayoutRuleResult ForFlow(
        BlockBox block,
        BlockFlowLayoutResult flow)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(flow);

        return new(
            block,
            flow.InlineLayout,
            flow.Children,
            flow.Flow);
    }
}
