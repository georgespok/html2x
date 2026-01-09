namespace Html2x.Abstractions.Measurements.Units;

/// <summary>
/// Represents a width and height in points.
/// </summary>
public readonly record struct SizePt(float Width, float Height)
{
    /// <summary>Returns a copy with non-finite values set to 0.</summary>
    public SizePt Safe()
        => new(SafeValue(Width), SafeValue(Height));

    /// <summary>Clamps width and height to minimum values.</summary>
    public SizePt ClampMin(float minWidth, float minHeight)
        => new(Math.Max(minWidth, Width), Math.Max(minHeight, Height));

    /// <summary>Clamps width and height to maximum values.</summary>
    public SizePt ClampMax(float maxWidth, float maxHeight)
        => new(Math.Min(maxWidth, Width), Math.Min(maxHeight, Height));

    /// <summary>Clamps width and height to a min and max range.</summary>
    public SizePt Clamp(float minWidth, float maxWidth, float minHeight, float maxHeight)
        => new(
            Math.Clamp(Width, minWidth, maxWidth),
            Math.Clamp(Height, minHeight, maxHeight));

    /// <summary>Returns a new size inflated by horizontal and vertical values.</summary>
    public SizePt Inflate(float horizontal, float vertical)
        => new(Width + horizontal, Height + vertical);

    /// <summary>Scales width and height by a factor.</summary>
    public SizePt Scale(float factor)
        => new(Width * factor, Height * factor);

    private static float SafeValue(float value)
        => float.IsFinite(value) ? value : 0f;
}
