using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Contracts.Published;

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