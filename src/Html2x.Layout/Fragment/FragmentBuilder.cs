using Html2x.Layout.Box;
using Html2x.Layout.Fragment.Stages;

namespace Html2x.Layout.Fragment;

public sealed class FragmentBuilder : IFragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildStage> _stages =
    [
        new BlockFragmentStage(),
        new InlineFragmentStage(),
        new SpecializedFragmentStage(),
        new ZOrderStage()
    ];

    private readonly IReadOnlyList<IFragmentBuildObserver> _observers;

    public FragmentBuilder()
        : this(Array.Empty<IFragmentBuildObserver>())
    {
    }

    public FragmentBuilder(IEnumerable<IFragmentBuildObserver> observers)
    {
        _observers = observers?.ToArray() ?? Array.Empty<IFragmentBuildObserver>();
    }

    public FragmentTree Build(BoxTree boxes)
    {
        var state = new FragmentBuildState(boxes).WithObservers(_observers);

        foreach (var stage in _stages)
        {
            state = stage.Execute(state);
        }

        return state.Fragments;
    }
}
