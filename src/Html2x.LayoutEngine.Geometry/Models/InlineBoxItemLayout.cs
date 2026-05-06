using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed record InlineBoxItemLayout(
    int Order,
    RectPt Rect,
    BlockBox ContentBox)
    : InlineLineItemLayout(Order, Rect);
