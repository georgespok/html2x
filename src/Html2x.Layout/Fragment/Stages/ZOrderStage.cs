using Html2x.Core.Layout;

namespace Html2x.Layout.Fragment.Stages;

public sealed class ZOrderStage : IFragmentBuildStage
{
    public void Execute(FragmentBuildContext context)
    {
        var all = context.Result.Blocks
            .SelectMany(Flatten)
            .OrderBy(f => f.ZOrder)
            .ToList();

        context.Result.All.Clear();
        context.Result.All.AddRange(all);
    }

    private static IEnumerable<Core.Layout.Fragment> Flatten(Core.Layout.Fragment fragment)
    {
        yield return fragment;

        if (fragment is BlockFragment block)
        {
            foreach (var child in block.Children)
            foreach (var sub in Flatten(child))
            {
                yield return sub;
            }
        }
    }
}