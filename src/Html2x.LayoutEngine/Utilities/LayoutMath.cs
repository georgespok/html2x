namespace Html2x.LayoutEngine.Utilities;

internal static class LayoutMath
{
    public static float Safe(float value)
    {
        return float.IsFinite(value) ? value : 0f;
    }
}
