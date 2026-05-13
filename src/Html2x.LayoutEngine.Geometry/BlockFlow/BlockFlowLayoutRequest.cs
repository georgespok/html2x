namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal sealed record BlockFlowLayoutRequest(
    BlockBox Parent,
    float ContentX,
    float CursorY,
    float ContentWidth,
    float ParentContentTop);
