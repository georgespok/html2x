namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedInlineFlowSegmentItem : PublishedBlockFlowItem
{
    public PublishedInlineFlowSegmentItem(int order, PublishedInlineFlowSegment segment)
        : base(order)
    {
        ArgumentNullException.ThrowIfNull(segment);

        Segment = segment;
    }

    public PublishedInlineFlowSegment Segment { get; }
}