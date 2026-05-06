using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;

namespace Html2x.LayoutEngine.Geometry.Test;

internal static class PublishedLayoutTestResolver
{
    public static PublishedLayoutTree Resolve(
        BlockBoxLayout blockBoxLayout,
        BoxNode boxRoot,
        PageBox page) =>
        new BoxTreeLayout(blockBoxLayout).Layout(boxRoot, page);
}