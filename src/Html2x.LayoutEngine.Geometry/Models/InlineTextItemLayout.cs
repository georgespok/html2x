using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Geometry.Models;


internal sealed record InlineTextItemLayout(
    int Order,
    RectPt Rect,
    IReadOnlyList<TextRun> Runs,
    IReadOnlyList<InlineBox> Sources)
    : InlineLineItemLayout(Order, Rect);
