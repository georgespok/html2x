using Html2x.RenderModel.Geometry;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

internal static class SkiaGeometryMapper
{
    public static SKRect ToSkRect(RectPt rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);
}