namespace Html2x.Abstractions.Layout.Fragments;

// Container/block-level box (flow or table cells can reuse this)
public class BlockFragment : Fragment
{
    private readonly List<Fragment> _children;

    public BlockFragment()
        : this([])
    {
    }

    public BlockFragment(IEnumerable<Fragment>? children)
    {
        _children = children?.ToList() ?? [];
    }

    public IReadOnlyList<Fragment> Children => _children;

    public FragmentDisplayRole? DisplayRole { get; init; }

    public FormattingContextKind? FormattingContext { get; init; }

    public float? MarkerOffset { get; init; }

    internal void AddChild(Fragment child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
    }
}
