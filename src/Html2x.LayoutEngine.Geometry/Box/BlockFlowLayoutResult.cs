using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Geometry.Box;

internal readonly record struct BlockFlowLayoutResult(
    float ContentHeight,
    IReadOnlyList<PublishedBlock> PublishedChildren,
    PublishedInlineLayout? PublishedInlineLayout,
    IReadOnlyList<PublishedBlockFlowItem> PublishedFlow);