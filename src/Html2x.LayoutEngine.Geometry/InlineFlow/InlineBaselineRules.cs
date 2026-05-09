namespace Html2x.LayoutEngine.Geometry.InlineFlow;

internal static class InlineBaselineRules
{
    public static float ResolveLineAscent(TextLayoutLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var ascent = 0f;
        foreach (var run in line.Runs)
        {
            ascent = Math.Max(ascent, run.Ascent);
        }

        return ascent;
    }
}
