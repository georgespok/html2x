using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.TestSupport;

internal static class FragmentTraversal
{
    public static IEnumerable<LayoutFragment> EnumerateFragments(IEnumerable<BlockFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            foreach (var nested in EnumerateFragments(fragment))
            {
                yield return nested;
            }
        }
    }

    public static IEnumerable<LayoutFragment> EnumerateFragments(IEnumerable<LayoutFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            foreach (var nested in EnumerateFragments(fragment))
            {
                yield return nested;
            }
        }
    }

    public static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateFragments(child))
            {
                yield return nested;
            }
        }
    }

    public static IEnumerable<LineBoxFragment> EnumerateLines(LayoutFragment fragment) =>
        EnumerateFragments(fragment).OfType<LineBoxFragment>();

    public static IEnumerable<LineBoxFragment> EnumerateLines(IEnumerable<LayoutFragment> fragments) =>
        EnumerateFragments(fragments).OfType<LineBoxFragment>();

    public static LineBoxFragment FindLine(IEnumerable<LineBoxFragment> lines, string text) =>
        lines.First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));

    public static LineBoxFragment FindLine(LayoutFragment fragment, string text) =>
        EnumerateLines(fragment)
            .First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));

    public static TextRun FindRun(BlockFragment fragment, string text) =>
        EnumerateLines(fragment)
            .SelectMany(line => line.Runs)
            .First(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> CollectTextRuns(IEnumerable<BlockFragment> fragments) =>
        EnumerateFragments(fragments)
            .OfType<LineBoxFragment>()
            .SelectMany(line => line.Runs)
            .Select(run => run.Text.Trim())
            .Where(text => !string.IsNullOrEmpty(text));
}
