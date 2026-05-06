namespace Html2x.LayoutEngine.Contracts.Style;

internal static class CssUnitConversion
{
    private const double PointsPerCssPixel = 72.0 / 96.0;
    private const double CssPixelsPerPoint = 96.0 / 72.0;

    public static float CssPxToPt(double cssPixels) =>
        double.IsFinite(cssPixels)
            ? (float)(cssPixels * PointsPerCssPixel)
            : 0f;

    public static float? CssPxToPtOrNull(double? cssPixels) =>
        cssPixels.HasValue && double.IsFinite(cssPixels.Value)
            ? CssPxToPt(cssPixels.Value)
            : null;

    public static double PtToCssPx(float points) =>
        float.IsFinite(points)
            ? points * CssPixelsPerPoint
            : 0d;
}