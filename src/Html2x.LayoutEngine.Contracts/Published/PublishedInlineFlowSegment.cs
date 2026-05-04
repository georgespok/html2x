namespace Html2x.LayoutEngine.Contracts.Published;

using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Geometry;


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
