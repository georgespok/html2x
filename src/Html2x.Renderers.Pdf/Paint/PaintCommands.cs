using System.Drawing;
using Html2x.RenderModel;

namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
/// Identifies the supported internal paint command categories for current PDF rendering.
/// </summary>
internal enum PaintCommandKind
{
    PageBackground,
    Background,
    Border,
    Text,
    Image,
    Rule
}

/// <summary>
/// Base metadata shared by all internal paint commands emitted for a layout page.
/// </summary>
internal abstract record PaintCommand(
    PaintCommandKind Kind,
    int PageNumber,
    int? SourceFragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex);

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
        new RectangleF(0f, 0f, PageSize.Width, PageSize.Height),
        ZOrder: int.MinValue,
        CommandIndex);

/// <summary>
/// Paints a block-like fragment background fill.
/// </summary>
internal sealed record BackgroundPaintCommand(
    int PageNumber,
    int FragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex,
    ColorRgba Color)
    : PaintCommand(PaintCommandKind.Background, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);

/// <summary>
/// Paints border edges for block-like and image fragments.
/// </summary>
internal sealed record BorderPaintCommand(
    int PageNumber,
    int FragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex,
    BorderEdges Borders)
    : PaintCommand(PaintCommandKind.Border, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);

/// <summary>
/// Paints one positioned text run with its font, metrics, color, and decorations.
/// </summary>
internal sealed record TextPaintCommand(
    int PageNumber,
    int FragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex,
    TextRun Run)
    : PaintCommand(PaintCommandKind.Text, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);

/// <summary>
/// Paints image content or its placeholder using the image fragment payload.
/// </summary>
internal sealed record ImagePaintCommand(
    int PageNumber,
    int FragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex,
    VisualStyle Style,
    string Src,
    RectangleF ContentRect,
    SizePx AuthoredSizePx,
    SizePx IntrinsicSizePx,
    bool IsMissing,
    bool IsOversize)
    : PaintCommand(PaintCommandKind.Image, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);

/// <summary>
/// Paints the current rule fragment line using its resolved top border style.
/// </summary>
internal sealed record RulePaintCommand(
    int PageNumber,
    int FragmentId,
    RectangleF Rect,
    int ZOrder,
    int CommandIndex,
    BorderSide? Border)
    : PaintCommand(PaintCommandKind.Rule, PageNumber, FragmentId, Rect, ZOrder, CommandIndex);
