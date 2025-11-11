using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class ZOrderStage : IFragmentBuildStage
{
    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var all = state.Fragments.Blocks
            .SelectMany(Flatten)
            .OrderBy(f => f.ZOrder)
            .ToList();

        state.Fragments.All.Clear();
        state.Fragments.All.AddRange(all);

        foreach (var observer in state.Observers)
        {
            observer.OnZOrderCompleted(all);
        }

        return state;
    }

    private static IEnumerable<Abstractions.Layout.Fragments.Fragment> Flatten(Abstractions.Layout.Fragments.Fragment fragment)
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
