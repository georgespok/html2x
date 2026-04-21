using System.Drawing;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public readonly record struct UsedGeometry(
    RectangleF BorderBoxRect,
    RectangleF ContentBoxRect,
    float? Baseline,
    float MarkerOffset,
    bool AllowsOverflow)
{
    public float X => BorderBoxRect.X;

    public float Y => BorderBoxRect.Y;

    public float Width => BorderBoxRect.Width;

    public float Height => BorderBoxRect.Height;

    public UsedGeometry Translate(float deltaX, float deltaY)
    {
        return this with
        {
            BorderBoxRect = Offset(BorderBoxRect, deltaX, deltaY),
            ContentBoxRect = Offset(ContentBoxRect, deltaX, deltaY),
            Baseline = Baseline.HasValue ? Baseline.Value + deltaY : null
        };
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

    public static UsedGeometry FromBorderBox(
        RectangleF borderBoxRect,
        Spacing padding,
        Spacing border,
        float? baseline = null,
        float markerOffset = 0f,
        bool allowsOverflow = false)
    {
        var contentRect = Inset(border.Add(padding), borderBoxRect);
        return new UsedGeometry(borderBoxRect, contentRect, baseline, markerOffset, allowsOverflow);
    }

    private UsedGeometry Resize(float borderWidth, float borderHeight)
    {
        var leftInset = ContentBoxRect.X - BorderBoxRect.X;
        var topInset = ContentBoxRect.Y - BorderBoxRect.Y;
        var rightInset = BorderBoxRect.Right - ContentBoxRect.Right;
        var bottomInset = BorderBoxRect.Bottom - ContentBoxRect.Bottom;

        return this with
        {
            BorderBoxRect = new RectangleF(BorderBoxRect.X, BorderBoxRect.Y, borderWidth, borderHeight),
            ContentBoxRect = new RectangleF(
                BorderBoxRect.X + leftInset,
                BorderBoxRect.Y + topInset,
                Math.Max(0f, borderWidth - leftInset - rightInset),
                Math.Max(0f, borderHeight - topInset - bottomInset))
        };
    }

    private static RectangleF Offset(RectangleF rect, float deltaX, float deltaY)
    {
        return new RectangleF(rect.X + deltaX, rect.Y + deltaY, rect.Width, rect.Height);
    }

    private static RectangleF Inset(Spacing inset, RectangleF rect)
    {
        var width = Math.Max(0f, rect.Width - inset.Left - inset.Right);
        var height = Math.Max(0f, rect.Height - inset.Top - inset.Bottom);
        return new RectangleF(rect.X + inset.Left, rect.Y + inset.Top, width, height);
    }
}
