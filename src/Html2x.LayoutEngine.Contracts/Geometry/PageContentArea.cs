using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Geometry;

internal readonly record struct PageContentArea(float X, float Y, float Width, float Height)
{
    public float Bottom => Y + Height;

    public static PageContentArea From(SizePt pageSize, Spacing margin)
    {
        var width = RequirePositiveFinite(pageSize.Width, nameof(pageSize));
        var height = RequirePositiveFinite(pageSize.Height, nameof(pageSize));
        var safeMargin = RequireMargin(margin);
        var contentWidth = Math.Max(0f, width - safeMargin.Left - safeMargin.Right);
        var contentHeight = Math.Max(0f, height - safeMargin.Top - safeMargin.Bottom);

        return new(
            safeMargin.Left,
            safeMargin.Top,
            contentWidth,
            contentHeight);
    }

    private static Spacing RequireMargin(Spacing margin) =>
        new(
            RequireNonNegativeFinite(margin.Top, nameof(margin)),
            RequireNonNegativeFinite(margin.Right, nameof(margin)),
            RequireNonNegativeFinite(margin.Bottom, nameof(margin)),
            RequireNonNegativeFinite(margin.Left, nameof(margin)));

    private static float RequirePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) ||
            value <= 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Page size must be finite and positive.");
        }

        return value;
    }

    private static float RequireNonNegativeFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) ||
            value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Page margin must be finite and non-negative.");
        }

        return value;
    }
}
