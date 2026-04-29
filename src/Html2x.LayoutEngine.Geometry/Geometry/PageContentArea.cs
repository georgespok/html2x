using Html2x.Abstractions.Layout.Geometry;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Geometry;

internal readonly record struct PageContentArea(float X, float Y, float Width, float Height)
{
    public float Bottom => Y + Height;

    public static PageContentArea From(SizePt pageSize, Spacing margin)
    {
        var width = RequireNonNegative(pageSize.Width, nameof(pageSize.Width));
        var height = RequireNonNegative(pageSize.Height, nameof(pageSize.Height));
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
            RequireNonNegative(margin.Top, nameof(margin.Top)),
            RequireNonNegative(margin.Right, nameof(margin.Right)),
            RequireNonNegative(margin.Bottom, nameof(margin.Bottom)),
            RequireNonNegative(margin.Left, nameof(margin.Left)));
    }

    private static float RequireNonNegative(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            return 0f;
        }

        return GeometryGuard.RequireNonNegativeFinite(name, Math.Max(0f, value));
    }
}
