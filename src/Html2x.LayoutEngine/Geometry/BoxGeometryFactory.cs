using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Geometry;

internal readonly record struct ContentArea(float X, float Y, float Width, float Height)
{
    public RectangleF Rect => new(X, Y, Width, Height);
}

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
            NormalizeNullableFinite(baseline),
            NormalizeMarkerOffset(markerOffset),
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
            NormalizeLayoutRect(borderBoxRect),
            NormalizeLayoutRect(contentBoxRect),
            NormalizeNullableFinite(baseline),
            NormalizeMarkerOffset(markerOffset),
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

    public static ContentArea ResolveContentArea(UsedGeometry geometry)
    {
        return ResolveContentArea(geometry.ContentBoxRect, geometry.MarkerOffset);
    }

    public static float ResolveContentWidth(
        float borderWidth,
        Spacing padding,
        Spacing border,
        float markerOffset = 0f)
    {
        if (float.IsPositiveInfinity(borderWidth))
        {
            return float.PositiveInfinity;
        }

        return ResolveContentArea(0f, 0f, borderWidth, 0f, padding, border, markerOffset).Width;
    }

    public static ContentArea ResolveContentArea(
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
        return ResolveContentArea(contentRect, markerOffset);
    }

    internal static RectangleF CreateLayoutRect(float x, float y, float width, float height)
    {
        return new RectangleF(
            NormalizeFinite(x),
            NormalizeFinite(y),
            NormalizeNonNegative(width),
            NormalizeNonNegative(height));
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

    internal static float NormalizeNonNegative(float value)
    {
        return Math.Max(0f, NormalizeFinite(value));
    }

    internal static float NormalizeMarkerOffset(float value)
    {
        return NormalizeNonNegative(value);
    }

    internal static float NormalizeFinite(float value)
    {
        return float.IsFinite(value) ? value : 0f;
    }

    internal static float? NormalizeNullableFinite(float? value)
    {
        return value.HasValue && float.IsFinite(value.Value) ? value.Value : null;
    }

    private static ContentArea ResolveContentArea(RectangleF contentRect, float markerOffset)
    {
        var safeRect = NormalizeLayoutRect(contentRect);
        var safeMarkerOffset = NormalizeMarkerOffset(markerOffset);
        return new ContentArea(
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
}
