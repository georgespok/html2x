using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.Renderers.Pdf.Paint;


/// <summary>
/// Paints the page background before any fragment-derived commands.
/// </summary>
internal sealed record PageBackgroundPaintCommand(
    int PageNumber,
    SizePt PageSize,
    ColorRgba Color,
    int CommandIndex)
    : PaintCommand(
        PaintCommandKind.PageBackground,
        PageNumber,
        SourceFragmentId: null,
        new RectPt(0f, 0f, PageSize.Width, PageSize.Height),
        ZOrder: int.MinValue,
        CommandIndex);
