namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry;


internal sealed record PublishedInlineObjectItem : PublishedInlineItem
{
    public PublishedInlineObjectItem(
        int order,
        RectPt rect,
        PublishedBlock content)
        : base(order, rect)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
    }

    public PublishedBlock Content { get; }
}
