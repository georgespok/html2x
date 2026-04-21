using Html2x.LayoutEngine.Fragment.Stages;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuilder : IFragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildObserver> _observers;
    private readonly FragmentAdapterRegistry _fragmentAdapters;

    public FragmentBuilder()
        : this([], FragmentAdapterRegistry.CreateDefault())
    {
    }

    public FragmentBuilder(IEnumerable<IFragmentBuildObserver> observers)
        : this(observers, FragmentAdapterRegistry.CreateDefault())
    {
    }

    internal FragmentBuilder(
        IEnumerable<IFragmentBuildObserver> observers,
        FragmentAdapterRegistry fragmentAdapters)
    {
        _observers = observers?.ToArray() ?? [];
        _fragmentAdapters = fragmentAdapters ?? throw new ArgumentNullException(nameof(fragmentAdapters));
    }

    public FragmentTree Build(BoxTree boxes, FragmentBuildContext context)
    {
        var state = new FragmentBuildState(boxes, context)
            .WithObservers(_observers);

        state = new BlockFragmentStage(_fragmentAdapters).Execute(state);
        state = new InlineFragmentStage(_fragmentAdapters).Execute(state);
        state = new SpecializedFragmentStage(_fragmentAdapters).Execute(state);
        state = new ZOrderStage().Execute(state);

        return state.Fragments;
    }
}
