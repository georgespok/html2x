using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Geometry;

internal readonly record struct UsedGeometry
{
    internal UsedGeometry(
        RectPt borderBoxRect,
        RectPt contentBoxRect,
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

    public RectPt BorderBoxRect { get; }

    public RectPt ContentBoxRect { get; }

    public float? Baseline { get; }

    public float MarkerOffset { get; }

    public bool AllowsOverflow { get; }

    public float X => BorderBoxRect.X;

    public float Y => BorderBoxRect.Y;

    public float Width => BorderBoxRect.Width;

    public float Height => BorderBoxRect.Height;

    public UsedGeometry Translate(float deltaX, float deltaY) =>
        new(
            Translate(BorderBoxRect, deltaX, deltaY),
            Translate(ContentBoxRect, deltaX, deltaY),
            Baseline.HasValue ? Baseline.Value + deltaY : null,
            MarkerOffset,
            AllowsOverflow);

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

    public UsedGeometry WithBorderWidth(float value) => Resize(value, BorderBoxRect.Height);

    public UsedGeometry WithBorderHeight(float value) => Resize(BorderBoxRect.Width, value);

    public UsedGeometry WithContentInsets(Spacing padding, Spacing border)
    {
        var contentInsets = border.Safe().Add(padding.Safe());
        return new(
            BorderBoxRect,
            Inset(BorderBoxRect, contentInsets),
            Baseline,
            MarkerOffset,
            AllowsOverflow);
    }

    public UsedGeometry WithMarkerOffset(float value) =>
        new(
            BorderBoxRect,
            ContentBoxRect,
            Baseline,
            value,
            AllowsOverflow);

    private UsedGeometry Resize(float borderWidth, float borderHeight)
    {
        var leftInset = ContentBoxRect.X - BorderBoxRect.X;
        var topInset = ContentBoxRect.Y - BorderBoxRect.Y;
        var rightInset = BorderBoxRect.Right - ContentBoxRect.Right;
        var bottomInset = BorderBoxRect.Bottom - ContentBoxRect.Bottom;
        var borderRect = new RectPt(
            BorderBoxRect.X,
            BorderBoxRect.Y,
            GuardNonNegativeFinite(nameof(borderWidth), borderWidth),
            GuardNonNegativeFinite(nameof(borderHeight), borderHeight));
        var contentRect = Inset(
            borderRect,
            new(topInset, rightInset, bottomInset, leftInset));

        return new(
            borderRect,
            contentRect,
            Baseline,
            MarkerOffset,
            AllowsOverflow);
    }

    private static RectPt Translate(RectPt rect, float deltaX, float deltaY)
    {
        GuardFinite(nameof(deltaX), deltaX);
        GuardFinite(nameof(deltaY), deltaY);

        return rect.Translate(deltaX, deltaY);
    }

    private static RectPt Inset(RectPt rect, Spacing inset) =>
        new(
            rect.X + inset.Left,
            rect.Y + inset.Top,
            Math.Max(0f, rect.Width - inset.Horizontal),
            Math.Max(0f, rect.Height - inset.Vertical));

    private static void GuardRect(string name, RectPt rect)
    {
        GuardFinite($"{name}.X", rect.X);
        GuardFinite($"{name}.Y", rect.Y);
        GuardNonNegativeFinite($"{name}.Width", rect.Width);
        GuardNonNegativeFinite($"{name}.Height", rect.Height);
    }

    private static void GuardNullableFinite(string name, float? value)
    {
        if (value.HasValue)
        {
            GuardFinite(name, value.Value);
        }
    }

    private static void GuardNonNegative(string name, float value)
    {
        GuardNonNegativeFinite(name, value);
    }

    private static float GuardNonNegativeFinite(string name, float value)
    {
        GuardFinite(name, value);
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }

        return value;
    }

    private static void GuardFinite(string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
    }
}