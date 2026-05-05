using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Published;

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