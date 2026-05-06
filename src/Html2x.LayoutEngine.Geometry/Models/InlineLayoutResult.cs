namespace Html2x.LayoutEngine.Geometry.Models;

/// <summary>
///     Captures measured inline layout segments and aggregate dimensions for a block content area.
/// </summary>
internal sealed record InlineLayoutResult(
    IReadOnlyList<InlineFlowSegmentLayout> Segments,
    float TotalHeight,
    float MaxLineWidth)
{
    public static InlineLayoutResult Empty { get; } = new([], 0f, 0f);
}