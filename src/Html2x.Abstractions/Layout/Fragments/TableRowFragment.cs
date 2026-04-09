namespace Html2x.Abstractions.Layout.Fragments;

public sealed class TableRowFragment : BlockFragment
{
    public TableRowFragment()
        : this([])
    {
    }

    public TableRowFragment(IEnumerable<TableCellFragment>? cells)
        : base(cells?.Cast<Fragment>())
    {
    }

    public int RowIndex { get; init; }

    public IReadOnlyList<TableCellFragment> Cells => Children.OfType<TableCellFragment>().ToList();
}
