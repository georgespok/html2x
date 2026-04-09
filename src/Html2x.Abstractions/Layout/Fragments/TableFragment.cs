namespace Html2x.Abstractions.Layout.Fragments;

public sealed class TableFragment : BlockFragment
{
    public TableFragment()
        : this([])
    {
    }

    public TableFragment(IEnumerable<TableRowFragment>? rows)
        : base(rows?.Cast<Fragment>())
    {
    }

    public int DerivedColumnCount { get; init; }

    public IReadOnlyList<TableRowFragment> Rows => Children.OfType<TableRowFragment>().ToList();
}
