using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
/// Centralizes geometry-owned translation for used geometry.
/// </summary>
internal static class GeometryTranslator
{
    internal static UsedGeometry Translate(UsedGeometry geometry, float deltaX, float deltaY)
    {
        return BoxGeometryFactory.FromResolvedBoxes(
            RenderGeometryTranslator.Translate(geometry.BorderBoxRect, deltaX, deltaY),
            RenderGeometryTranslator.Translate(geometry.ContentBoxRect, deltaX, deltaY),
            geometry.Baseline.HasValue ? geometry.Baseline.Value + deltaY : null,
            geometry.MarkerOffset,
            geometry.AllowsOverflow);
    }
}
