using Html2x.RenderModel.Geometry;

namespace Html2x.RenderModel.Fragments;

/// <summary>
///     Centralizes fragment rectangle validation so published geometry rejects non-finite or negative dimensions.
/// </summary>
internal static class FragmentGeometryGuard
{
    public static void GuardRect(string name, RectPt rect)
    {
        RequireFinite($"{name}.X", rect.X);
        RequireFinite($"{name}.Y", rect.Y);
        RequireNonNegativeFinite($"{name}.Width", rect.Width);
        RequireNonNegativeFinite($"{name}.Height", rect.Height);
    }

    public static PointPt RequirePoint(string name, PointPt point)
    {
        RequireFinite($"{name}.X", point.X);
        RequireFinite($"{name}.Y", point.Y);
        return point;
    }

    public static float RequireNonNegativeFinite(string name, float value)
    {
        RequireFinite(name, value);
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be non-negative.");
        }

        return value;
    }

    public static float RequireFinite(string name, float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite.");
        }

        return value;
    }
}