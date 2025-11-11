using Html2x.LayoutEngine.Box;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildState
{
    public FragmentBuildState(BoxTree boxes)
        : this(boxes, new FragmentTree(), [], Array.Empty<IFragmentBuildObserver>())
    {
    }

    private FragmentBuildState(BoxTree boxes, FragmentTree fragments, IReadOnlyList<BlockFragmentBinding> blockBindings,
        IReadOnlyList<IFragmentBuildObserver> observers)
    {
        Boxes = boxes ?? throw new ArgumentNullException(nameof(boxes));
        Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        BlockBindings = blockBindings ?? [];
        Observers = observers ?? Array.Empty<IFragmentBuildObserver>();
    }

    public BoxTree Boxes { get; }

    public FragmentTree Fragments { get; }

    public IReadOnlyList<BlockFragmentBinding> BlockBindings { get; }

    public IReadOnlyList<IFragmentBuildObserver> Observers { get; }

    public FragmentBuildState WithBlockBindings(IReadOnlyList<BlockFragmentBinding> bindings)
    {
        return new FragmentBuildState(Boxes, Fragments, bindings ?? [], Observers);
    }

    public FragmentBuildState WithObservers(IReadOnlyList<IFragmentBuildObserver> observers)
    {
        return new FragmentBuildState(Boxes, Fragments, BlockBindings,
            observers ?? Array.Empty<IFragmentBuildObserver>());
    }
}