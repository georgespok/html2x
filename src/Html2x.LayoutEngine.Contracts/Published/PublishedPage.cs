namespace Html2x.LayoutEngine.Geometry.Published;

using Html2x.RenderModel;

internal sealed record PublishedPage
{
    public PublishedPage(SizePt size, Spacing margin)
    {
        PublishedLayoutGuard.ThrowIfNegativeOrNonFinite(size, nameof(size));
        PublishedLayoutGuard.ThrowIfNonFinite(margin, nameof(margin));

        Size = size;
        Margin = margin;
    }

    public SizePt Size { get; }

    public Spacing Margin { get; }
}