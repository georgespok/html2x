using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Centralizes supported border-box and content-box dimension policy.
/// </summary>
internal static class BoxDimensionResolver
{
    public static float ResolveBlockBorderBoxWidth(
        ComputedStyle style,
        float availableWidth,
        Spacing margin)
    {
        ArgumentNullException.ThrowIfNull(style);

        var width = style.WidthPt ?? Math.Max(0f, availableWidth - margin.Left - margin.Right);
        return ApplyWidthConstraints(width, style);
    }

    public static float ResolveAtomicBorderBoxWidth(ComputedStyle style, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(style);

        var width = style.WidthPt ?? availableWidth;
        return ApplyWidthConstraints(width, style);
    }

    public static float ResolveIntrinsicBorderBoxWidth(ComputedStyle style, float measuredBorderBoxWidth)
    {
        ArgumentNullException.ThrowIfNull(style);

        var width = style.WidthPt ?? measuredBorderBoxWidth;
        return BoxGeometryFactory.NormalizeNonNegative(ApplyWidthConstraints(width, style));
    }

    public static float ResolveContentBoxWidth(
        float borderBoxWidth,
        Spacing padding,
        Spacing border,
        float markerOffset = 0f)
    {
        return BoxGeometryFactory.ResolveContentWidth(borderBoxWidth, padding, border, markerOffset);
    }

    public static float ResolveContentBoxHeight(
        ComputedStyle style,
        float measuredContentHeight,
        float minimumContentHeight = 0f)
    {
        ArgumentNullException.ThrowIfNull(style);

        var contentHeight = Math.Max(minimumContentHeight, measuredContentHeight);

        if (style.HeightPt.HasValue)
        {
            contentHeight = style.HeightPt.Value;
        }

        if (style.MinHeightPt.HasValue)
        {
            contentHeight = Math.Max(contentHeight, style.MinHeightPt.Value);
        }

        if (style.MaxHeightPt.HasValue)
        {
            contentHeight = Math.Min(contentHeight, style.MaxHeightPt.Value);
        }

        return BoxGeometryFactory.NormalizeNonNegative(contentHeight);
    }

    public static float ApplyWidthConstraints(float width, ComputedStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        var constrained = width;
        if (style.MinWidthPt.HasValue)
        {
            constrained = Math.Max(constrained, style.MinWidthPt.Value);
        }

        if (style.MaxWidthPt.HasValue)
        {
            constrained = Math.Min(constrained, style.MaxWidthPt.Value);
        }

        return constrained;
    }
}
