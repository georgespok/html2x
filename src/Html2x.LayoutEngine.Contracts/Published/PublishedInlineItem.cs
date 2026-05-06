using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Contracts.Published;

internal abstract record PublishedInlineItem
{
    protected PublishedInlineItem(int order, RectPt rect)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        Order = order;
        Rect = PublishedLayoutGuard.RequireRect(rect, nameof(rect));
    }

    public int Order { get; }

    public RectPt Rect { get; }
}