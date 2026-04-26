namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Represents a table cell fragment that can own block or inline child fragments.
/// </summary>
public sealed class TableCellFragment : BlockFragment
{
    public TableCellFragment()
        : this([])
    {
    }

    public TableCellFragment(IEnumerable<Fragment>? children)
        : base(children)
    {
    }

    public int ColumnIndex { get; init; }

    public bool IsHeader { get; init; }
}
