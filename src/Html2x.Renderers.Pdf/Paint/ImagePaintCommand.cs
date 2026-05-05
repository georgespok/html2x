using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

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
    ImageLoadStatus Status)
    : PaintCommand(PaintCommandKind.Image, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);
