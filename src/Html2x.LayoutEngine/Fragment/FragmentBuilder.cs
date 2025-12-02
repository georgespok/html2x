using Html2x.LayoutEngine.Fragment.Stages;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuilder(IEnumerable<IFragmentBuildObserver> observers) : IFragmentBuilder
{
    private readonly IReadOnlyList<IFragmentBuildObserver> _observers = observers?.ToArray() ?? [];

    public FragmentBuilder()
        : this([])
    {
    }

    public FragmentTree Build(BoxTree boxes, FragmentBuildContext context)
    {
        var state = new FragmentBuildState(boxes, context)
            .WithObservers(_observers);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);
        state = new SpecializedFragmentStage().Execute(state);
        state = new ZOrderStage().Execute(state);

        return state.Fragments;
    }
}
