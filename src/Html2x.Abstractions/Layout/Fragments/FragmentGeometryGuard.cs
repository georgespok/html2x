using System.Drawing;

namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Centralizes fragment rectangle validation so published geometry rejects non-finite or negative dimensions.
/// </summary>
internal static class FragmentGeometryGuard
{
    public static void GuardRect(string name, RectangleF rect)
    {
        GuardFinite($"{name}.X", rect.X);
        GuardFinite($"{name}.Y", rect.Y);
        GuardNonNegative($"{name}.Width", rect.Width);
        GuardNonNegative($"{name}.Height", rect.Height);
    }

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
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }
    }
}
