using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.RenderModel.Geometry;

/// <summary>
/// Represents a rectangle in PDF points.
/// </summary>
public readonly record struct RectPt
{
    /// <summary>
    /// Initializes a new rectangle in PDF points.
    /// </summary>
    public RectPt(float x, float y, float width, float height)
    {
        X = RequireFinite(nameof(x), x);
        Y = RequireFinite(nameof(y), y);
        Width = RequireNonNegativeFinite(nameof(width), width);
        Height = RequireNonNegativeFinite(nameof(height), height);
    }

    /// <summary>
    /// Gets the empty rectangle at the origin.
    /// </summary>
    public static RectPt Empty { get; } = new(0f, 0f, 0f, 0f);

    /// <summary>
    /// Gets the x coordinate in PDF points.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the y coordinate in PDF points.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Gets the width in PDF points.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the height in PDF points.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets the left edge in PDF points.
    /// </summary>
    public float Left => X;

    /// <summary>
    /// Gets the top edge in PDF points.
    /// </summary>
    public float Top => Y;

    /// <summary>
    /// Gets the right edge in PDF points.
    /// </summary>
    public float Right => X + Width;

    /// <summary>
    /// Gets the bottom edge in PDF points.
    /// </summary>
    public float Bottom => Y + Height;

    /// <summary>
    /// Gets the rectangle size in PDF points.
    /// </summary>
    public SizePt Size => new(Width, Height);

    /// <summary>
    /// Gets the rectangle origin in PDF points.
    /// </summary>
    public PointPt Origin => new(X, Y);

    /// <summary>
    /// Returns a rectangle translated by the supplied offsets.
    /// </summary>
    public RectPt Translate(float deltaX, float deltaY)
        => new(X + RequireFinite(nameof(deltaX), deltaX), Y + RequireFinite(nameof(deltaY), deltaY), Width, Height);

    /// <summary>
    /// Returns a rectangle with a different x coordinate.
    /// </summary>
    public RectPt WithX(float value)
        => new(value, Y, Width, Height);

    /// <summary>
    /// Returns a rectangle with a different y coordinate.
    /// </summary>
    public RectPt WithY(float value)
        => new(X, value, Width, Height);

    /// <summary>
    /// Returns a rectangle with a different width.
    /// </summary>
    public RectPt WithWidth(float value)
        => new(X, Y, value, Height);

    /// <summary>
    /// Returns a rectangle with a different height.
    /// </summary>
    public RectPt WithHeight(float value)
        => new(X, Y, Width, value);

    /// <summary>
    /// Returns a rectangle inset by the supplied spacing and clamps dimensions at zero.
    /// </summary>
    public RectPt Inset(Spacing spacing)
        => new(
            X + spacing.Left,
            Y + spacing.Top,
            Math.Max(0f, Width - spacing.Horizontal),
            Math.Max(0f, Height - spacing.Vertical));

    private static float RequireNonNegativeFinite(string name, float value)
    {
        RequireFinite(name, value);
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }

        return value;
    }

    private static float RequireFinite(string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }

        return value;
    }
}
