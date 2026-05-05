namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedInlineFlowSegment
{
    public PublishedInlineFlowSegment(
        IReadOnlyList<PublishedInlineLine> lines,
        float top,
        float height)
    {
        Lines = PublishedLayoutGuard.CopyList(lines, nameof(lines));
        Top = PublishedLayoutGuard.RequireFinite(top, nameof(top));
        Height = PublishedLayoutGuard.RequireNonNegativeFinite(height, nameof(height));
    }

    public IReadOnlyList<PublishedInlineLine> Lines { get; }

    public float Top { get; }

    public float Height { get; }
}
