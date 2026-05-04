namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry;


internal sealed record PublishedInlineLine
{
    public PublishedInlineLine(
        int lineIndex,
        RectPt rect,
        RectPt occupiedRect,
        float baselineY,
        float lineHeight,
        string? textAlign,
        IReadOnlyList<PublishedInlineItem> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);

        LineIndex = lineIndex;
        Rect = PublishedLayoutGuard.RequireRect(rect, nameof(rect));
        OccupiedRect = PublishedLayoutGuard.RequireRect(occupiedRect, nameof(occupiedRect));
        BaselineY = PublishedLayoutGuard.RequireFinite(baselineY, nameof(baselineY));
        LineHeight = PublishedLayoutGuard.RequireNonNegativeFinite(lineHeight, nameof(lineHeight));
        TextAlign = textAlign;
        Items = PublishedLayoutGuard.CopyList(items, nameof(items));
    }

    public int LineIndex { get; }

    public RectPt Rect { get; }

    public RectPt OccupiedRect { get; }

    public float BaselineY { get; }

    public float LineHeight { get; }

    public string? TextAlign { get; }

    public IReadOnlyList<PublishedInlineItem> Items { get; }
}
