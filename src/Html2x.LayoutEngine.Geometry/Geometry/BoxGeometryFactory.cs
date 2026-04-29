using System.Drawing;
using Html2x.Abstractions.Layout.Geometry;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Geometry;

internal readonly record struct ContentFlowArea(float X, float Y, float Width, float Height);

internal static class BoxGeometryFactory
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
        bool allowsOverflow = false)
    {
        return FromBorderBox(
            CreateLayoutRect(x, y, width, height),
            padding,
            border,
            baseline,
            markerOffset,
            allowsOverflow);
    }

    public static UsedGeometry FromBorderBox(
        RectangleF borderBoxRect,
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
        RectangleF borderBoxRect,
        RectangleF contentBoxRect,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false)
    {
        return new UsedGeometry(
            ValidateLayoutRect(nameof(borderBoxRect), borderBoxRect),
            ValidateLayoutRect(nameof(contentBoxRect), contentBoxRect),
            ValidateNullableFinite(nameof(baseline), baseline),
            ValidateNonNegativeFinite(nameof(markerOffset), markerOffset),
            allowsOverflow);
    }

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
            new Spacing(topInset, rightInset, bottomInset, leftInset));

        return FromResolvedBoxes(
            borderRect,
            contentRect,
            geometry.Baseline,
            geometry.MarkerOffset,
            geometry.AllowsOverflow);
    }

    internal static UsedGeometry WithMarkerOffset(UsedGeometry geometry, float markerOffset)
    {
        return FromResolvedBoxes(
            geometry.BorderBoxRect,
            geometry.ContentBoxRect,
            geometry.Baseline,
            markerOffset,
            geometry.AllowsOverflow);
    }

    public static ContentFlowArea ResolveContentFlowArea(UsedGeometry geometry)
    {
        return ResolveContentFlowArea(geometry.ContentBoxRect, geometry.MarkerOffset);
    }

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

    public static float ResolveBorderBoxHeight(float contentHeight, Spacing padding, Spacing border)
    {
        return RequireNonNegativeFinite(contentHeight) + padding.Safe().Vertical + border.Safe().Vertical;
    }

    public static UsedGeometry FromBorderBoxWithContentHeight(
        float x,
        float y,
        float width,
        float contentHeight,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false)
    {
        return FromBorderBox(
            x,
            y,
            width,
            ResolveBorderBoxHeight(contentHeight, padding, border),
            padding,
            border,
            baseline,
            markerOffset,
            allowsOverflow);
    }

    internal static RectangleF CreateLayoutRect(float x, float y, float width, float height)
    {
        GeometryGuard.RequireFinite(nameof(x), x);
        GeometryGuard.RequireFinite(nameof(y), y);
        GeometryGuard.RequireNonNegativeFinite(nameof(width), width);
        GeometryGuard.RequireNonNegativeFinite(nameof(height), height);

        return new RectangleF(
            x,
            y,
            width,
            height);
    }

    internal static RectangleF NormalizeLayoutRect(RectangleF rect)
    {
        return CreateLayoutRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    internal static RectangleF ResolveContentRect(
        RectangleF borderBoxRect,
        Spacing padding,
        Spacing border)
    {
        return Inset(NormalizeLayoutRect(borderBoxRect), border.Safe().Add(padding.Safe()));
    }

    internal static float RequireNonNegativeFinite(float value)
    {
        return GeometryGuard.RequireNonNegativeFinite(nameof(value), value);
    }

    internal static float RequireMarkerOffset(float value)
    {
        return GeometryGuard.RequireNonNegativeFinite(nameof(value), value);
    }

    internal static float RequireFinite(float value)
    {
        return GeometryGuard.RequireFinite(nameof(value), value);
    }

    internal static float? RequireNullableFinite(float? value)
    {
        return GeometryGuard.RequireNullableFinite(nameof(value), value);
    }

    private static ContentFlowArea ResolveContentFlowArea(RectangleF contentRect, float markerOffset)
    {
        var safeRect = NormalizeLayoutRect(contentRect);
        var safeMarkerOffset = RequireMarkerOffset(markerOffset);
        return new ContentFlowArea(
            safeRect.X + safeMarkerOffset,
            safeRect.Y,
            Math.Max(0f, safeRect.Width - safeMarkerOffset),
            safeRect.Height);
    }

    private static RectangleF Inset(RectangleF rect, Spacing inset)
    {
        return new RectangleF(
            rect.X + inset.Left,
            rect.Y + inset.Top,
            Math.Max(0f, rect.Width - inset.Left - inset.Right),
            Math.Max(0f, rect.Height - inset.Top - inset.Bottom));
    }

    private static RectangleF ValidateLayoutRect(string name, RectangleF rect)
    {
        return GeometryGuard.RequireRect(name, rect);
    }

    private static float? ValidateNullableFinite(string name, float? value)
    {
        return GeometryGuard.RequireNullableFinite(name, value);
    }

    private static float ValidateNonNegativeFinite(string name, float value)
    {
        return GeometryGuard.RequireNonNegativeFinite(name, value);
    }

    private static float ValidateFinite(string name, float value)
    {
        return GeometryGuard.RequireFinite(name, value);
    }
}
