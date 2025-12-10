using System.Drawing;

namespace Html2x.Abstractions.Rendering;

public sealed class RenderInstruction
{
    private readonly RectangleF _geometry;

    public int FragmentId { get; init; }

    public int PageNumber { get; init; }

    public DrawCommand Command { get; init; }

    public RectangleF Geometry
    {
        get => _geometry;
        init
        {
            GuardFinite(nameof(Geometry.X), value.X);
            GuardFinite(nameof(Geometry.Y), value.Y);
            GuardNonNegative(nameof(Geometry.Width), value.Width);
            GuardNonNegative(nameof(Geometry.Height), value.Height);
            _geometry = value;
        }
    }

    public object? Payload { get; init; }

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
