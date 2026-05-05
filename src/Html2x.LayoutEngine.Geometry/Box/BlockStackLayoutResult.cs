using Html2x.LayoutEngine.Contracts.Published;

namespace Html2x.LayoutEngine.Geometry.Box;


internal sealed record BlockStackLayoutResult(
    IReadOnlyList<BlockBox> Blocks,
    IReadOnlyList<PublishedBlock> PublishedBlocks);
