using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Box;

internal static class BlockPlacementService
{
    public static PointPt ResolveOrigin(BlockLayoutRequest request, Spacing margin)
    {
        var rawX = request.ContentX + margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;

        return new PointPt(
            Math.Max(rawX, request.ContentX),
            Math.Max(rawY, request.ParentContentTop));
    }
}
