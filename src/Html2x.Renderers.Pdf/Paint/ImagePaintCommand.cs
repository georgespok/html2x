using Html2x.RenderModel;

namespace Html2x.Renderers.Pdf.Paint;


/// <summary>
/// Paints image content or its placeholder using the image fragment payload.
/// </summary>
internal sealed record ImagePaintCommand(
    int PageNumber,
    int FragmentId,
    RectPt Rect,
    int ZOrder,
    int CommandIndex,
    VisualStyle Style,
    string Src,
    RectPt ContentRect,
    SizePx AuthoredSizePx,
    SizePx IntrinsicSizePx,
    ImageLoadStatus Status,
    bool IsMissing,
    bool IsOversize)
    : PaintCommand(PaintCommandKind.Image, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);
