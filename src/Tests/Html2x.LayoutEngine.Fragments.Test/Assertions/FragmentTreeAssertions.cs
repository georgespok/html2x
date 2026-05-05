using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Fragments.Test.Assertions;

internal static class FragmentTreeAssertions
{
    public static IReadOnlyList<LayoutFragment> Flatten(FragmentTree tree)
    {
        return tree.Blocks
            .SelectMany(static block => Enumerate(block).Prepend(block))
            .ToList();
    }

    public static IReadOnlyList<string> EnumerateText(LayoutFragment fragment)
    {
        return Enumerate(fragment)
            .Prepend(fragment)
            .OfType<LineBoxFragment>()
            .SelectMany(static line => line.Runs)
            .Select(static run => run.Text)
            .ToList();
    }

    private static IEnumerable<LayoutFragment> Enumerate(LayoutFragment fragment)
    {
        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            yield return child;
            foreach (var nested in Enumerate(child))
            {
                yield return nested;
            }
        }
    }
}
