using Html2x.RenderModel.Geometry;

namespace Html2x.Renderers.Pdf.Paint;


/// <summary>
/// Base metadata shared by all internal paint commands emitted for a layout page.
/// </summary>
internal abstract record PaintCommand(
    PaintCommandKind Kind,
    int PageNumber,
    int? SourceFragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex);
