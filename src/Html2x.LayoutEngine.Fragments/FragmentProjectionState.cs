using Html2x.LayoutEngine.Contracts.Published;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Fragments;

internal sealed class FragmentProjectionState(int pageNumber)
{
    private readonly List<PublishedBlockFragmentBinding> _blockBindings = [];

    private readonly Dictionary<PublishedBlock, BlockFragment> _blockFragments = new(
        ReferenceEqualityComparer<PublishedBlock>.Instance);

    private readonly HashSet<PublishedBlock> _flowVisited = new(
        ReferenceEqualityComparer<PublishedBlock>.Instance);

    private readonly HashSet<PublishedBlock> _specialVisited = new(
        ReferenceEqualityComparer<PublishedBlock>.Instance);

    private int _nextFragmentId = 1;

    public int PageNumber { get; } = pageNumber;

    public IReadOnlyList<PublishedBlockFragmentBinding> BlockBindings => _blockBindings;

    public int ReserveFragmentId() => _nextFragmentId++;

    public void BindBlock(PublishedBlock block, BlockFragment fragment)
    {
        _blockBindings.Add(new(block, fragment));
        _blockFragments[block] = fragment;
    }

    public BlockFragment? FindBlockFragment(PublishedBlock block) =>
        _blockFragments.TryGetValue(block, out var fragment)
            ? fragment
            : null;

    public bool VisitFlowBlock(PublishedBlock block) => _flowVisited.Add(block);

    public bool VisitSpecialBlock(PublishedBlock block) => _specialVisited.Add(block);
}
