namespace Html2x.RenderModel.Fragments;

/// <summary>
/// Represents a block-level fragment that can own child fragments in normal flow or table cell content.
/// </summary>
public class BlockFragment(IEnumerable<Fragment>? children) : Fragment
{
    private readonly List<Fragment> _children = children?.ToList() ?? [];

    public BlockFragment()
        : this([])
    {
    }

    public IReadOnlyList<Fragment> Children => _children;

    public FragmentDisplayRole? DisplayRole { get; init; }

    public FormattingContextKind? FormattingContext { get; init; }

    public float? MarkerOffset { get; init; }

    internal void AddChild(Fragment child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        OnChildAdded(child);
    }

    protected virtual void OnChildAdded(Fragment child)
    {
    }
}
