using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;

internal static class BlockOriginRules
{
    public static PointPt ResolveOrigin(BlockLayoutRequest request, Spacing margin)
    {
        var rawX = request.ContentX + margin.Left;
        var rawY = request.CursorY + request.CollapsedTopMargin;

        return new(
            Math.Max(rawX, request.ContentX),
            Math.Max(rawY, request.ParentContentTop));
    }
}