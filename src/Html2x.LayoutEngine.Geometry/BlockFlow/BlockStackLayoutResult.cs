namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal sealed record BlockStackLayoutResult(
    IReadOnlyList<BlockBox> Blocks,
    IReadOnlyList<BlockLayoutRuleResult> Results);
