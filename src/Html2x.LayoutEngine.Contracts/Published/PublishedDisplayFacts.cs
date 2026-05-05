using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Contracts.Published;

internal sealed record PublishedDisplayFacts
{
    public PublishedDisplayFacts(
        FragmentDisplayRole role,
        FormattingContextKind formattingContext,
        float? markerOffset)
    {
        PublishedLayoutGuard.ThrowIfUndefined(role, nameof(role));
        PublishedLayoutGuard.ThrowIfUndefined(formattingContext, nameof(formattingContext));
        PublishedLayoutGuard.ThrowIfNegativeOrNonFinite(markerOffset, nameof(markerOffset));

        Role = role;
        FormattingContext = formattingContext;
        MarkerOffset = markerOffset;
    }

    public FragmentDisplayRole Role { get; }

    public FormattingContextKind FormattingContext { get; }

    public float? MarkerOffset { get; }
}
