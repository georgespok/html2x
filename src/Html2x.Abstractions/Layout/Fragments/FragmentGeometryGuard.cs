using System.Drawing;
using Html2x.Abstractions.Layout.Geometry;

namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Centralizes fragment rectangle validation so published geometry rejects non-finite or negative dimensions.
/// </summary>
internal static class FragmentGeometryGuard
{
    public static void GuardRect(string name, RectangleF rect)
    {
        GeometryGuard.RequireRect(name, rect);
    }
}
