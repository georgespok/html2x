namespace Html2x.RenderModel;

/// <summary>
/// Represents a block-level fragment that can own child fragments in normal flow or table cell content.
/// </summary>
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
        OnChildAdded(child);
    }

    protected virtual void OnChildAdded(Fragment child)
    {
    }
}
