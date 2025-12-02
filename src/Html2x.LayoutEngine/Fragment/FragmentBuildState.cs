using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildState
{
    public FragmentBuildState(BoxTree boxes, FragmentBuildContext context)
        : this(boxes, new FragmentTree(), [], [], context)
    {
    }

    private FragmentBuildState(
        BoxTree boxes,
        FragmentTree fragments,
        IReadOnlyList<BlockFragmentBinding> blockBindings,
        IReadOnlyList<IFragmentBuildObserver> observers,
        FragmentBuildContext context)
    {
        Boxes = boxes ?? throw new ArgumentNullException(nameof(boxes));
        Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        BlockBindings = blockBindings ?? [];
        Observers = observers ?? [];
        Context = context;
    }

    public BoxTree Boxes { get; }

    public FragmentTree Fragments { get; }

    public IReadOnlyList<BlockFragmentBinding> BlockBindings { get; }

    public IReadOnlyList<IFragmentBuildObserver> Observers { get; }

    public FragmentBuildContext Context { get; }

    public FragmentBuildState WithBlockBindings(IReadOnlyList<BlockFragmentBinding> bindings)
    {
        return new FragmentBuildState(Boxes, Fragments, bindings ?? [], Observers, Context);
    }

    public FragmentBuildState WithObservers(IReadOnlyList<IFragmentBuildObserver> observers)
    {
        return new FragmentBuildState(Boxes, Fragments, BlockBindings,
            observers ?? [], Context);
    }
}
