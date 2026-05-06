using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Primitives;

internal static class UsedGeometryRules
{
    public static UsedGeometry FromBorderBox(
        float x,
        float y,
        float width,
        float height,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false) =>
        FromBorderBox(
            CreateLayoutRect(x, y, width, height),
            padding,
            border,
            baseline,
            markerOffset,
            allowsOverflow);

    public static UsedGeometry FromBorderBox(
        RectPt borderBoxRect,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false)
    {
        var borderRect = NormalizeLayoutRect(borderBoxRect);
        var safePadding = padding.Safe();
        var safeBorder = border.Safe();
        var contentRect = ResolveContentRect(borderRect, safePadding, safeBorder);

        return FromResolvedBoxes(
            borderRect,
            contentRect,
            baseline,
            markerOffset,
            allowsOverflow);
    }

    internal static UsedGeometry FromResolvedBoxes(
        RectPt borderBoxRect,
        RectPt contentBoxRect,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false) =>
        new(
            ValidateLayoutRect(nameof(borderBoxRect), borderBoxRect),
            ValidateLayoutRect(nameof(contentBoxRect), contentBoxRect),
            ValidateNullableFinite(nameof(baseline), baseline),
            ValidateNonNegativeFinite(nameof(markerOffset), markerOffset),
            allowsOverflow);

    internal static UsedGeometry WithBorderSize(
        UsedGeometry geometry,
        float borderWidth,
        float borderHeight)
    {
        var leftInset = geometry.ContentBoxRect.X - geometry.BorderBoxRect.X;
        var topInset = geometry.ContentBoxRect.Y - geometry.BorderBoxRect.Y;
        var rightInset = geometry.BorderBoxRect.Right - geometry.ContentBoxRect.Right;
        var bottomInset = geometry.BorderBoxRect.Bottom - geometry.ContentBoxRect.Bottom;
        var borderRect = CreateLayoutRect(
            geometry.BorderBoxRect.X,
            geometry.BorderBoxRect.Y,
            borderWidth,
            borderHeight);
        var contentRect = Inset(
            borderRect,
            new(topInset, rightInset, bottomInset, leftInset));

        return FromResolvedBoxes(
            borderRect,
            contentRect,
            geometry.Baseline,
            geometry.MarkerOffset,
            geometry.AllowsOverflow);
    }

    internal static UsedGeometry WithMarkerOffset(UsedGeometry geometry, float markerOffset) =>
        FromResolvedBoxes(
            geometry.BorderBoxRect,
            geometry.ContentBoxRect,
            geometry.Baseline,
            markerOffset,
            geometry.AllowsOverflow);

    public static ContentFlowArea ResolveContentFlowArea(UsedGeometry geometry) =>
        ResolveContentFlowArea(geometry.ContentBoxRect, geometry.MarkerOffset);

    public static float ResolveContentFlowWidth(
        float borderWidth,
        Spacing padding,
        Spacing border,
        float markerOffset = 0f)
    {
        if (float.IsPositiveInfinity(borderWidth))
        {
            return float.PositiveInfinity;
        }

        return ResolveContentFlowArea(0f, 0f, borderWidth, 0f, padding, border, markerOffset).Width;
    }

    public static ContentFlowArea ResolveContentFlowArea(
        float x,
        float y,
        float width,
        float height,
        Spacing padding,
        Spacing border,
        float markerOffset = 0f)
    {
        var borderRect = CreateLayoutRect(x, y, width, height);
        var contentRect = ResolveContentRect(borderRect, padding.Safe(), border.Safe());
        return ResolveContentFlowArea(contentRect, markerOffset);
    }

    public static float ResolveBorderBoxHeight(float contentHeight, Spacing padding, Spacing border) =>
        RequireNonNegativeFinite(contentHeight) + padding.Safe().Vertical + border.Safe().Vertical;

    public static UsedGeometry FromBorderBoxWithContentHeight(
        float x,
        float y,
        float width,
        float contentHeight,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false) =>
        FromBorderBox(
            x,
            y,
            width,
            ResolveBorderBoxHeight(contentHeight, padding, border),
            padding,
            border,
            baseline,
            markerOffset,
            allowsOverflow);

    internal static RectPt CreateLayoutRect(float x, float y, float width, float height)
    {
        GeometryGuard.RequireFinite(nameof(x), x);
        GeometryGuard.RequireFinite(nameof(y), y);
        GeometryGuard.RequireNonNegativeFinite(nameof(width), width);
        GeometryGuard.RequireNonNegativeFinite(nameof(height), height);

        return new(
            x,
            y,
            width,
            height);
    }

    internal static RectPt NormalizeLayoutRect(RectPt rect) =>
        CreateLayoutRect(rect.X, rect.Y, rect.Width, rect.Height);

    internal static RectPt ResolveContentRect(
        RectPt borderBoxRect,
        Spacing padding,
        Spacing border) =>
        Inset(NormalizeLayoutRect(borderBoxRect), border.Safe().Add(padding.Safe()));

    internal static float RequireNonNegativeFinite(float value) =>
        GeometryGuard.RequireNonNegativeFinite(nameof(value), value);

    internal static float RequireMarkerOffset(float value) =>
        GeometryGuard.RequireNonNegativeFinite(nameof(value), value);

    internal static float RequireFinite(float value) => GeometryGuard.RequireFinite(nameof(value), value);

    internal static float? RequireNullableFinite(float? value) =>
        GeometryGuard.RequireNullableFinite(nameof(value), value);

    private static ContentFlowArea ResolveContentFlowArea(RectPt contentRect, float markerOffset)
    {
        var safeRect = NormalizeLayoutRect(contentRect);
        var safeMarkerOffset = RequireMarkerOffset(markerOffset);
        return new(
            safeRect.X + safeMarkerOffset,
            safeRect.Y,
            Math.Max(0f, safeRect.Width - safeMarkerOffset),
            safeRect.Height);
    }

    private static RectPt Inset(RectPt rect, Spacing inset) =>
        new(
            rect.X + inset.Left,
            rect.Y + inset.Top,
            Math.Max(0f, rect.Width - inset.Left - inset.Right),
            Math.Max(0f, rect.Height - inset.Top - inset.Bottom));

    private static RectPt ValidateLayoutRect(string name, RectPt rect) => GeometryGuard.RequireRect(name, rect);

    private static float? ValidateNullableFinite(string name, float? value) =>
        GeometryGuard.RequireNullableFinite(name, value);

    private static float ValidateNonNegativeFinite(string name, float value) =>
        GeometryGuard.RequireNonNegativeFinite(name, value);
}