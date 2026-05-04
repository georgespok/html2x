using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Geometry.Models;


internal sealed record InlineObjectItemLayout(
    int Order,
    RectPt Rect,
    BlockBox ContentBox)
    : InlineLineItemLayout(Order, Rect);
