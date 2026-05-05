using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Geometry.Box;

internal sealed record BlockLayoutRuleResult(
    BlockBox Block,
    PublishedInlineLayout? InlineLayout,
    IReadOnlyList<PublishedBlock> Children,
    IReadOnlyList<PublishedBlockFlowItem>? Flow)
{
    public static BlockLayoutRuleResult ForResolvedBlock(BlockBox block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new BlockLayoutRuleResult(block, null, [], null);
    }

    public static BlockLayoutRuleResult ForFlow(
        BlockBox block,
        BlockFlowLayoutResult flow)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(flow);

        return new BlockLayoutRuleResult(
            block,
            flow.PublishedInlineLayout,
            flow.PublishedChildren,
            flow.PublishedFlow);
    }
}
