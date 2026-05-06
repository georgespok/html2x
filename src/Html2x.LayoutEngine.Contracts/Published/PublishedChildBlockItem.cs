namespace Html2x.LayoutEngine.Contracts.Published;

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