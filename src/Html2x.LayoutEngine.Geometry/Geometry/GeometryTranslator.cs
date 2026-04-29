using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
/// Centralizes value-level geometry translation for rectangles, text runs, and used geometry.
/// </summary>
internal static class GeometryTranslator
{
    internal static RectangleF Translate(RectangleF rect, float deltaX, float deltaY)
    {
        return new RectangleF(rect.X + deltaX, rect.Y + deltaY, rect.Width, rect.Height);
    }

    internal static TextRun Translate(TextRun run, float deltaX, float deltaY)
    {
        return run with
        {
            Origin = new PointF(run.Origin.X + deltaX, run.Origin.Y + deltaY)
        };
    }

    internal static UsedGeometry Translate(UsedGeometry geometry, float deltaX, float deltaY)
    {
        return BoxGeometryFactory.FromResolvedBoxes(
            Translate(geometry.BorderBoxRect, deltaX, deltaY),
            Translate(geometry.ContentBoxRect, deltaX, deltaY),
            geometry.Baseline.HasValue ? geometry.Baseline.Value + deltaY : null,
            geometry.MarkerOffset,
            geometry.AllowsOverflow);
    }
}
