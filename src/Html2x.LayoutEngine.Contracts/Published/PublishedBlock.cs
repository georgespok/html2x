namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.RenderModel;

/// <summary>
/// Carries the immutable block facts that fragment projection consumes after layout geometry completes.
/// </summary>
internal sealed record PublishedBlock
{
    public PublishedBlock(
        PublishedBlockIdentity identity,
        PublishedDisplayFacts display,
        VisualStyle style,
        UsedGeometry geometry,
        PublishedInlineLayout? inlineLayout,
        PublishedImageFacts? image,
        PublishedRuleFacts? rule,
        PublishedTableFacts? table,
        IReadOnlyList<PublishedBlock> children,
        IReadOnlyList<PublishedBlockFlowItem>? flow = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(style);

        Identity = identity;
        Display = display;
        Style = style;
        Geometry = geometry;
        InlineLayout = inlineLayout;
        Image = image;
        Rule = rule;
        Table = table;
        Children = PublishedLayoutGuard.CopyList(children, nameof(children));
        Flow = flow is null
            ? CreateDefaultFlow(InlineLayout, Children)
            : PublishedLayoutGuard.CopyList(flow, nameof(flow));
    }

    public PublishedBlockIdentity Identity { get; }

    public PublishedDisplayFacts Display { get; }

    public VisualStyle Style { get; }

    public UsedGeometry Geometry { get; }

    public PublishedInlineLayout? InlineLayout { get; }

    public PublishedImageFacts? Image { get; }

    public PublishedRuleFacts? Rule { get; }

    public PublishedTableFacts? Table { get; }

    public IReadOnlyList<PublishedBlock> Children { get; }

    public IReadOnlyList<PublishedBlockFlowItem> Flow { get; }

    private static IReadOnlyList<PublishedBlockFlowItem> CreateDefaultFlow(
        PublishedInlineLayout? inlineLayout,
        IReadOnlyList<PublishedBlock> children)
    {
        var flow = new List<PublishedBlockFlowItem>();
        var order = 0;

        if (inlineLayout is not null)
        {
            foreach (var segment in inlineLayout.Segments)
            {
                flow.Add(new PublishedInlineFlowSegmentItem(order++, segment));
            }
        }

        foreach (var child in children)
        {
            flow.Add(new PublishedChildBlockItem(order++, child));
        }

        return Array.AsReadOnly(flow.ToArray());
    }
}
