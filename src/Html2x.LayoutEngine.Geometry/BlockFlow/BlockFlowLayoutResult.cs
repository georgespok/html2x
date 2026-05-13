namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal readonly record struct BlockFlowLayoutResult(
    float ContentHeight,
    IReadOnlyList<BlockLayoutRuleResult> Children,
    InlineLayoutResult? InlineLayout,
    IReadOnlyList<BlockFlowItemLayout> Flow);
