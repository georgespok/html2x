using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Measurement;

/// <summary>
///     Carries inline content size facts without implying layout segment output.
/// </summary>
internal readonly record struct InlineContentSizeFacts
{
    public InlineContentSizeFacts(float totalHeight, float maxLineWidth)
    {
        TotalHeight = UsedGeometryRules.RequireNonNegativeFinite(totalHeight);
        MaxLineWidth = UsedGeometryRules.RequireNonNegativeFinite(maxLineWidth);
    }

    public float TotalHeight { get; }

    public float MaxLineWidth { get; }
}
