namespace Html2x.RenderModel.Fragments;

/// <summary>
///     Represents a table cell fragment that can own block or inline child fragments.
/// </summary>
public sealed class TableCellFragment(IEnumerable<Fragment>? children) : BlockFragment(children)
{
    public TableCellFragment()
        : this([])
    {
    }

    public int ColumnIndex { get; init; }

    public bool IsHeader { get; init; }
}