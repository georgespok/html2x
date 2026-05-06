using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
///     Paints a block-like fragment background fill.
/// </summary>
internal sealed record BackgroundPaintCommand(
    int PageNumber,
    int FragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex,
    ColorRgba Color)
    : PaintCommand(PaintCommandKind.Background, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);