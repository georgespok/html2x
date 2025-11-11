using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Fragment.Stages;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuilder(IEnumerable<IFragmentBuildObserver> observers) : IFragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildStage> _stages =
    [
        new BlockFragmentStage(),
        new InlineFragmentStage(),
        new SpecializedFragmentStage(),
        new ZOrderStage()
    ];

    private readonly IReadOnlyList<IFragmentBuildObserver> _observers = observers?.ToArray() ?? [];

    public FragmentBuilder()
        : this([])
    {
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
