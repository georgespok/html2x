using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Layout.Fragments;

public abstract class Fragment
{
    private readonly RectangleF _rect;
    private readonly VisualStyle _style = new();

    public int FragmentId { get; init; }

    public int PageNumber { get; init; }

    public RectangleF Rect
    {
        get => _rect;
        init
        {
            GuardFinite(nameof(Rect.X), value.X);
            GuardFinite(nameof(Rect.Y), value.Y);
            GuardNonNegative(nameof(Rect.Width), value.Width);
            GuardNonNegative(nameof(Rect.Height), value.Height);
            _rect = value;
        }
    } // absolute page coords (pt)

    public SizePt Size => new(Rect.Width, Rect.Height);

    public int ZOrder { get; init; } // resolved stacking/z-index

    public VisualStyle Style
    {
        get => _style;
        init => _style = value ?? new VisualStyle();
    } // minimal style needed to paint this box

    private static void GuardFinite(string name, float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }
    }

    private static void GuardNonNegative(string name, float value)
    {
        GuardFinite(name, value);
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }
    }
}
