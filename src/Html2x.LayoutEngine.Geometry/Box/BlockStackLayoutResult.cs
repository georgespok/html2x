namespace Html2x.LayoutEngine.Geometry.Box;

internal sealed record BlockStackLayoutResult(
    IReadOnlyList<BlockBox> Blocks,
    IReadOnlyList<BlockLayoutRuleResult> Results);
