using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box.Publishing;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Box;


internal sealed record BlockStackLayoutResult(
    IReadOnlyList<BlockBox> Blocks,
    IReadOnlyList<PublishedBlock> PublishedBlocks);
