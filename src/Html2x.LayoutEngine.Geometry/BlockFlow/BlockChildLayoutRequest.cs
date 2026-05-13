namespace Html2x.LayoutEngine.Geometry.BlockFlow;

internal sealed record BlockChildLayoutRequest
{
    public BlockChildLayoutRequest(
        BlockBox parent,
        float contentX,
        float cursorY,
        float contentWidth,
        float parentContentTop)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        ContentX = contentX;
        CursorY = cursorY;
        ContentWidth = contentWidth;
        ParentContentTop = parentContentTop;
    }

    public BlockBox Parent { get; }

    public float ContentX { get; }

    public float CursorY { get; }

    public float ContentWidth { get; }

    public float ParentContentTop { get; }
}
