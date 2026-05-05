namespace Html2x.LayoutEngine.Geometry.Models;


/// <summary>
/// Groups contiguous inline lines that share a coordinate space within a block.
/// </summary>
internal sealed record InlineFlowSegmentLayout(
    IReadOnlyList<InlineLineLayout> Lines,
    float Top,
    float Height);
