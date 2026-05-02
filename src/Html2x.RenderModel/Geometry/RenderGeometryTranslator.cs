using System.Drawing;

namespace Html2x.RenderModel;

/// <summary>
/// Translates already-published render geometry without changing size or style facts.
/// </summary>
public static class RenderGeometryTranslator
{
    /// <summary>
    /// Returns a rectangle translated by the supplied offsets while preserving size.
    /// </summary>
    public static RectangleF Translate(RectangleF rect, float deltaX, float deltaY)
    {
        return new RectangleF(rect.X + deltaX, rect.Y + deltaY, rect.Width, rect.Height);
    }

    /// <summary>
    /// Returns a text run translated by the supplied offsets while preserving metrics.
    /// </summary>
    public static TextRun Translate(TextRun run, float deltaX, float deltaY)
    {
        ArgumentNullException.ThrowIfNull(run);

        return run with
        {
            Origin = new PointF(run.Origin.X + deltaX, run.Origin.Y + deltaY)
        };
    }
}
