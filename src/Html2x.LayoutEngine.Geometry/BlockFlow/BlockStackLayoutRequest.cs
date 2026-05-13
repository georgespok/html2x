namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal sealed record BlockStackLayoutRequest(
    IReadOnlyList<BoxNode> Candidates,
    float ContentX,
    float ContentY,
    float ContentWidth);
