namespace Html2x.LayoutEngine.Style;

internal static class CssUnitConversion
{
    private const double PointsPerCssPixel = 72.0 / 96.0;
    private const double CssPixelsPerPoint = 96.0 / 72.0;

    public static float CssPxToPt(double cssPixels)
    {
        return double.IsFinite(cssPixels)
            ? (float)(cssPixels * PointsPerCssPixel)
            : 0f;
    }

    public static float? CssPxToPtOrNull(double? cssPixels)
    {
        return cssPixels.HasValue && double.IsFinite(cssPixels.Value)
            ? CssPxToPt(cssPixels.Value)
            : null;
    }

    public static double PtToCssPx(float points)
    {
        return float.IsFinite(points)
            ? points * CssPixelsPerPoint
            : 0d;
    }
}
