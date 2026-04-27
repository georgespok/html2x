using System.Drawing;
using Html2x.Abstractions.Layout.Geometry;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Models;

public readonly record struct UsedGeometry
{
    internal UsedGeometry(
        RectangleF borderBoxRect,
        RectangleF contentBoxRect,
        float? baseline,
        float markerOffset,
        bool allowsOverflow)
    {
        GuardRect(nameof(BorderBoxRect), borderBoxRect);
        GuardRect(nameof(ContentBoxRect), contentBoxRect);
        GuardNullableFinite(nameof(Baseline), baseline);
        GuardNonNegative(nameof(MarkerOffset), markerOffset);

        BorderBoxRect = borderBoxRect;
        ContentBoxRect = contentBoxRect;
        Baseline = baseline;
        MarkerOffset = markerOffset;
        AllowsOverflow = allowsOverflow;
    }

    public RectangleF BorderBoxRect { get; }

    public RectangleF ContentBoxRect { get; }

    public float? Baseline { get; }

    public float MarkerOffset { get; }

    public bool AllowsOverflow { get; }

    public float X => BorderBoxRect.X;

    public float Y => BorderBoxRect.Y;

    public float Width => BorderBoxRect.Width;

    public float Height => BorderBoxRect.Height;

    public UsedGeometry Translate(float deltaX, float deltaY)
    {
        return GeometryTranslator.Translate(this, deltaX, deltaY);
    }

    public UsedGeometry WithBorderX(float value)
    {
        var delta = value - BorderBoxRect.X;
        return Translate(delta, 0f);
    }

    public UsedGeometry WithBorderY(float value)
    {
        var delta = value - BorderBoxRect.Y;
        return Translate(0f, delta);
    }

    public UsedGeometry WithBorderWidth(float value)
    {
        return Resize(value, BorderBoxRect.Height);
    }

    public UsedGeometry WithBorderHeight(float value)
    {
        return Resize(BorderBoxRect.Width, value);
    }

    public UsedGeometry WithContentInsets(Spacing padding, Spacing border)
    {
        return BoxGeometryFactory.FromBorderBox(
            BorderBoxRect,
            padding,
            border,
            Baseline,
            MarkerOffset,
            AllowsOverflow);
    }

    public UsedGeometry WithMarkerOffset(float value)
    {
        return BoxGeometryFactory.WithMarkerOffset(this, value);
    }

    private UsedGeometry Resize(float borderWidth, float borderHeight)
    {
        return BoxGeometryFactory.WithBorderSize(this, borderWidth, borderHeight);
    }

    private static void GuardRect(string name, RectangleF rect)
    {
        GeometryGuard.RequireRect(name, rect);
    }

    private static void GuardNullableFinite(string name, float? value)
    {
        GeometryGuard.RequireNullableFinite(name, value);
    }

    private static void GuardNonNegative(string name, float value)
    {
        GeometryGuard.RequireNonNegativeFinite(name, value);
    }

    private static void GuardFinite(string name, float value)
    {
        GeometryGuard.RequireFinite(name, value);
    }
}
