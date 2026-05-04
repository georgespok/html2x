namespace Html2x.RenderModel;

/// <summary>
/// Represents a point in PDF points.
/// </summary>
public readonly record struct PointPt
{
    /// <summary>
    /// Initializes a new point in PDF points.
    /// </summary>
    public PointPt(float x, float y)
    {
        X = RequireFinite(nameof(x), x);
        Y = RequireFinite(nameof(y), y);
    }

    /// <summary>
    /// Gets the origin point.
    /// </summary>
    public static PointPt Zero { get; } = new(0f, 0f);

    /// <summary>
    /// Gets the x coordinate in PDF points.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// Gets the y coordinate in PDF points.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// Returns a point translated by the supplied offsets.
    /// </summary>
    public PointPt Translate(float deltaX, float deltaY)
        => new(X + RequireFinite(nameof(deltaX), deltaX), Y + RequireFinite(nameof(deltaY), deltaY));

    private static float RequireFinite(string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }

        return value;
    }
}
