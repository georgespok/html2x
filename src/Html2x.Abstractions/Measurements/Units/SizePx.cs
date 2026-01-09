namespace Html2x.Abstractions.Measurements.Units;

/// <summary>
/// Represents a width and height in CSS pixels.
/// Width or height can be null when a dimension is unspecified.
/// </summary>
public readonly record struct SizePx(double? Width, double? Height)
{
    public bool HasWidth => Width.HasValue;
    public bool HasHeight => Height.HasValue;

    public double WidthOrZero => Width ?? 0d;
    public double HeightOrZero => Height ?? 0d;

    public SizePx ClampMin(double minWidth, double minHeight)
        => new(Math.Max(minWidth, WidthOrZero), Math.Max(minHeight, HeightOrZero));

    public SizePx Scale(double factor)
        => new(Width.HasValue ? Width.Value * factor : null,
            Height.HasValue ? Height.Value * factor : null);
}
