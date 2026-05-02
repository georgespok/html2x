using System.Drawing;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Box;

internal static class BlockPlacementService
{
    public static PointF ResolveOrigin(BlockLayoutRequest request, Spacing margin)
    {
        var rawX = request.ContentX + margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;

        return new PointF(
            Math.Max(rawX, request.ContentX),
            Math.Max(rawY, request.ParentContentTop));
    }
}
