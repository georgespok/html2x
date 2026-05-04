using Html2x.RenderModel;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Centralizes supported border-box and content-box dimension policy.
/// </summary>
internal static class BoxDimensionResolver
{
    public static float ResolveBlockBorderBoxWidth(
        ComputedStyle style,
        float availableWidth,
        Spacing margin,
        Spacing padding,
        Spacing border)
    {
        ArgumentNullException.ThrowIfNull(style);

        if (style.WidthPt.HasValue)
        {
            return ResolveBorderBoxWidthFromContent(
                ApplyContentWidthConstraints(style.WidthPt.Value, style),
                padding,
                border);
        }

        var availableBorderBoxWidth = Math.Max(0f, availableWidth - margin.Left - margin.Right);
        var contentBoxWidth = ResolveContentBoxWidthFromBorder(availableBorderBoxWidth, padding, border);
        return ResolveBorderBoxWidthFromContent(
            ApplyContentWidthConstraints(contentBoxWidth, style),
            padding,
            border);
    }

    public static float ResolveAtomicBorderBoxWidth(
        ComputedStyle style,
        float availableWidth,
        Spacing padding,
        Spacing border)
    {
        ArgumentNullException.ThrowIfNull(style);

        if (style.WidthPt.HasValue)
        {
            return ResolveBorderBoxWidthFromContent(
                ApplyContentWidthConstraints(style.WidthPt.Value, style),
                padding,
                border);
        }

        var availableBorderBoxWidth = Math.Max(0f, availableWidth);
        var contentBoxWidth = ResolveContentBoxWidthFromBorder(availableBorderBoxWidth, padding, border);
        return ResolveBorderBoxWidthFromContent(
            ApplyContentWidthConstraints(contentBoxWidth, style),
            padding,
            border);
    }

    public static float ResolveIntrinsicBorderBoxWidth(
        ComputedStyle style,
        float measuredContentBoxWidth,
        Spacing padding,
        Spacing border)
    {
        ArgumentNullException.ThrowIfNull(style);

        var contentBoxWidth = style.WidthPt ?? measuredContentBoxWidth;
        return ResolveBorderBoxWidthFromContent(
            ApplyContentWidthConstraints(contentBoxWidth, style),
            padding,
            border);
    }

    public static float ResolveContentFlowWidth(
        float borderBoxWidth,
        Spacing padding,
        Spacing border,
        float markerOffset = 0f)
    {
        return BoxGeometryFactory.ResolveContentFlowWidth(borderBoxWidth, padding, border, markerOffset);
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

        return BoxGeometryFactory.RequireNonNegativeFinite(contentHeight);
    }

    public static float ApplyContentWidthConstraints(float contentBoxWidth, ComputedStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        var constrained = float.IsPositiveInfinity(contentBoxWidth)
            ? float.PositiveInfinity
            : BoxGeometryFactory.RequireNonNegativeFinite(contentBoxWidth);
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

    public static float ResolveBorderBoxWidthFromContent(
        float contentBoxWidth,
        Spacing padding,
        Spacing border)
    {
        if (float.IsPositiveInfinity(contentBoxWidth))
        {
            return float.PositiveInfinity;
        }

        return BoxGeometryFactory.RequireNonNegativeFinite(contentBoxWidth) +
               padding.Safe().Horizontal +
               border.Safe().Horizontal;
    }

    public static float ResolveContentBoxWidthFromBorder(
        float borderBoxWidth,
        Spacing padding,
        Spacing border)
    {
        if (float.IsPositiveInfinity(borderBoxWidth))
        {
            return float.PositiveInfinity;
        }

        return Math.Max(0f, borderBoxWidth - padding.Safe().Horizontal - border.Safe().Horizontal);
    }
}
