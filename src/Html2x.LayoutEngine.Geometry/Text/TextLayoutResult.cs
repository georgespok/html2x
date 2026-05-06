namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Carries all wrapped lines and aggregate inline layout metrics.
/// </summary>
internal sealed record TextLayoutResult(
    IReadOnlyList<TextLayoutLine> Lines,
    float TotalHeight,
    float MaxLineWidth);