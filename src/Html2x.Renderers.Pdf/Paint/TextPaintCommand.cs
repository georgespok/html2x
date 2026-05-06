using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
///     Paints one positioned text run with its font, metrics, color, and decorations.
/// </summary>
internal sealed record TextPaintCommand(
    int PageNumber,
    int FragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex,
    TextRun Run)
    : PaintCommand(PaintCommandKind.Text, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);