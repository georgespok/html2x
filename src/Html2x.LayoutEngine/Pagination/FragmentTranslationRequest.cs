using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Carries the target page and offset used when translating a fragment subtree for pagination.
/// </summary>
internal readonly record struct FragmentTranslationRequest(
    int PageNumber,
    float DeltaX,
    float DeltaY)
{
    public static FragmentTranslationRequest FromPlacement(
        LayoutFragment source,
        int pageNumber,
        float x,
        float y)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new FragmentTranslationRequest(
            pageNumber,
            x - source.Rect.X,
            y - source.Rect.Y);
    }
}
