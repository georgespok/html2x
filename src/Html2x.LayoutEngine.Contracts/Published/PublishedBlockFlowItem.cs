namespace Html2x.LayoutEngine.Geometry.Published;

/// <summary>
/// Describes one fragment-projection item in a published block's child flow.
/// </summary>
internal abstract record PublishedBlockFlowItem
{
    protected PublishedBlockFlowItem(int order)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        Order = order;
    }

    public int Order { get; }
}

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

internal sealed record PublishedChildBlockItem : PublishedBlockFlowItem
{
    public PublishedChildBlockItem(int order, PublishedBlock block)
        : base(order)
    {
        ArgumentNullException.ThrowIfNull(block);

        Block = block;
    }

    public PublishedBlock Block { get; }
}
