namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedInlineLayout
{
    public PublishedInlineLayout(
        IReadOnlyList<PublishedInlineFlowSegment> segments,
        float totalHeight,
        float maxLineWidth)
    {
        Segments = PublishedLayoutGuard.CopyList(segments, nameof(segments));
        TotalHeight = PublishedLayoutGuard.RequireNonNegativeFinite(totalHeight, nameof(totalHeight));
        MaxLineWidth = PublishedLayoutGuard.RequireNonNegativeFinite(maxLineWidth, nameof(maxLineWidth));
    }

    public IReadOnlyList<PublishedInlineFlowSegment> Segments { get; }

    public float TotalHeight { get; }

    public float MaxLineWidth { get; }
}
