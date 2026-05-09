namespace Html2x.LayoutEngine.Geometry.Box;

internal readonly record struct BlockFlowLayoutResult(
    float ContentHeight,
    IReadOnlyList<BlockLayoutRuleResult> Children,
    InlineLayoutResult? InlineLayout,
    IReadOnlyList<BlockFlowItemLayout> Flow);
