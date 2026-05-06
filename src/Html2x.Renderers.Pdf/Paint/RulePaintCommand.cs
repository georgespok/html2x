using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
///     Paints the current rule fragment line using its resolved top border style.
/// </summary>
internal sealed record RulePaintCommand(
    int PageNumber,
    int FragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex,
    BorderSide? Border)
    : PaintCommand(PaintCommandKind.Rule, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);