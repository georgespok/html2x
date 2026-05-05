namespace Html2x.LayoutEngine.Geometry.Box;

internal readonly record struct BlockLayoutRequest(
    float ContentX,
    float CursorY,
    float ContentWidth,
    float ParentContentTop,
    float PreviousBottomMargin,
    float CollapsedTopMargin);
