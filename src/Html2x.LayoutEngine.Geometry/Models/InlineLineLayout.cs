using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Geometry.Models;


/// <summary>
/// Describes one inline line where Rect is the line slot and OccupiedRect is the tight item bounds.
/// </summary>
internal sealed record InlineLineLayout(
    int LineIndex,
    RectPt Rect,
    RectPt OccupiedRect,
    float BaselineY,
    float LineHeight,
    string? TextAlign,
    IReadOnlyList<InlineLineItemLayout> Items);
