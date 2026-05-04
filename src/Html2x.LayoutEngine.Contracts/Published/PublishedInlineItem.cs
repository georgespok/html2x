namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry;


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
