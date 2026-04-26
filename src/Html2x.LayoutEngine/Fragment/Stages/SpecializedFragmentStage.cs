using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment.Stages;

public sealed class SpecializedFragmentStage : IFragmentBuildStage
{
    private readonly FragmentAdapterRegistry _fragmentAdapters;

    public SpecializedFragmentStage()
        : this(FragmentAdapterRegistry.CreateDefault())
    {
    }

    internal SpecializedFragmentStage(FragmentAdapterRegistry fragmentAdapters)
    {
        _fragmentAdapters = fragmentAdapters ?? throw new ArgumentNullException(nameof(fragmentAdapters));
    }

    public FragmentBuildState Execute(FragmentBuildState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.BlockBindings.Count == 0)
        {
            return state;
        }

        var lookup = state.BlockBindings.ToDictionary(b => b.Source, b => b.Fragment);

        foreach (var binding in state.BlockBindings)
        {
            AppendSpecializedFragments(state, binding.Source, binding.Fragment, lookup, state.Observers, _fragmentAdapters);
        }

        return state;
    }

    private void AppendSpecializedFragments(
        FragmentBuildState state,
        BlockBox blockBox,
        BlockFragment blockFragment,
        IReadOnlyDictionary<BlockBox, BlockFragment> lookup,
        IReadOnlyList<IFragmentBuildObserver> observers,
        FragmentAdapterRegistry fragmentAdapters)
    {
        var ownFragment = CreateSpecialFragment(state, blockBox, fragmentAdapters);
        if (ownFragment is not null)
        {
            blockFragment.AddChild(ownFragment);
            NotifySpecial(blockBox, ownFragment, observers);
        }

        foreach (var child in blockBox.Children)
        {
            if (child is BlockBox nested && lookup.TryGetValue(nested, out var nestedFragment))
            {
                AppendSpecializedFragments(state, nested, nestedFragment, lookup, observers, fragmentAdapters);
            }
        }
    }

    private static Abstractions.Layout.Fragments.Fragment? CreateSpecialFragment(
        FragmentBuildState state,
        DisplayNode child,
        FragmentAdapterRegistry fragmentAdapters)
    {
        if (fragmentAdapters.TryCreateSpecialFragment(child, state, out var fragment))
        {
            return fragment;
        }

        return null;
    }

    private static void NotifySpecial(DisplayNode source, Abstractions.Layout.Fragments.Fragment fragment,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        foreach (var observer in observers)
        {
            observer.OnSpecialFragmentCreated(source, fragment);
        }
    }
}
