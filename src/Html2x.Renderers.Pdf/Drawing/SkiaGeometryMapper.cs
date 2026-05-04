using Html2x.RenderModel;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

internal static class SkiaGeometryMapper
{
    public static SKRect ToSKRect(RectPt rect)
    {
        return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }
}
