using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Fragment;

public sealed class FragmentBuildState
{
    public FragmentBuildState(BoxTree boxes, FragmentBuildContext context)
        : this(boxes, new FragmentTree(), [], [], context, 1, 1)
    {
    }

    private FragmentBuildState(
        BoxTree boxes,
        FragmentTree fragments,
        IReadOnlyList<BlockFragmentBinding> blockBindings,
        IReadOnlyList<IFragmentBuildObserver> observers,
        FragmentBuildContext context,
        int pageNumber,
        int nextFragmentId)
    {
        Boxes = boxes ?? throw new ArgumentNullException(nameof(boxes));
        Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        BlockBindings = blockBindings ?? [];
        Observers = observers ?? [];
        Context = context;
        PageNumber = pageNumber;
        NextFragmentId = nextFragmentId;
    }

    public BoxTree Boxes { get; }

    public FragmentTree Fragments { get; }

    public IReadOnlyList<BlockFragmentBinding> BlockBindings { get; }

    public IReadOnlyList<IFragmentBuildObserver> Observers { get; }

    public FragmentBuildContext Context { get; }

    public int PageNumber { get; }

    public int NextFragmentId { get; private set; }

    public int ReserveFragmentId()
    {
        return NextFragmentId++;
    }

    public FragmentBuildState WithBlockBindings(IReadOnlyList<BlockFragmentBinding> bindings)
    {
        return new FragmentBuildState(Boxes, Fragments, bindings ?? [], Observers, Context, PageNumber, NextFragmentId);
    }

    public FragmentBuildState WithObservers(IReadOnlyList<IFragmentBuildObserver> observers)
    {
        return new FragmentBuildState(Boxes, Fragments, BlockBindings,
            observers ?? [], Context, PageNumber, NextFragmentId);
    }
}
