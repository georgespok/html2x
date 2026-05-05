using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Geometry;

internal readonly record struct PageContentArea(float X, float Y, float Width, float Height)
{
    public float Bottom => Y + Height;

    public static PageContentArea From(SizePt pageSize, Spacing margin)
    {
        var width = RequireNonNegative(pageSize.Width);
        var height = RequireNonNegative(pageSize.Height);
        var safeMargin = NormalizeMargin(margin);
        var contentWidth = Math.Max(0f, width - safeMargin.Left - safeMargin.Right);
        var contentHeight = Math.Max(0f, height - safeMargin.Top - safeMargin.Bottom);

        return new PageContentArea(
            safeMargin.Left,
            safeMargin.Top,
            contentWidth,
            contentHeight);
    }

    private static Spacing NormalizeMargin(Spacing margin)
    {
        return new Spacing(
            RequireNonNegative(margin.Top),
            RequireNonNegative(margin.Right),
            RequireNonNegative(margin.Bottom),
            RequireNonNegative(margin.Left));
    }

    private static float RequireNonNegative(float value)
    {
        if (!float.IsFinite(value))
        {
            return 0f;
        }

        return Math.Max(0f, value);
    }
}
