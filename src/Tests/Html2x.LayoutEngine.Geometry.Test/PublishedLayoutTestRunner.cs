using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.BlockFlow;

namespace Html2x.LayoutEngine.Geometry.Test;

internal static class PublishedLayoutTestRunner
{
    public static PublishedLayoutTree Run(
        BlockBoxLayout blockBoxLayout,
        BoxNode boxRoot,
        PageBox page) =>
        new BoxTreeLayout(blockBoxLayout).Layout(boxRoot, page);
}
