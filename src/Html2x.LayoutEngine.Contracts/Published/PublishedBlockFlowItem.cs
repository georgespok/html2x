namespace Html2x.LayoutEngine.Contracts.Published;

/// <summary>
///     Describes one fragment-projection item in a published block's child flow.
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