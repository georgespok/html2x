using Html2x.RenderModel;

namespace Html2x.Renderers.Pdf.Paint;


/// <summary>
/// Paints border edges for block-like and image fragments.
/// </summary>
internal sealed record BorderPaintCommand(
    int PageNumber,
    int FragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex,
    BorderEdges Borders)
    : PaintCommand(PaintCommandKind.Border, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);
