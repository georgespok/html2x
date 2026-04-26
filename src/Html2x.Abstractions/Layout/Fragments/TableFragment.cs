namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Represents a table fragment with row children and derived table metadata.
/// </summary>
public sealed class TableFragment : BlockFragment
{
    private readonly List<TableRowFragment> _rows;

    public TableFragment()
        : this([])
    {
    }

    public TableFragment(IEnumerable<TableRowFragment>? rows)
        : this(MaterializeRows(rows))
    {
    }

    private TableFragment(List<TableRowFragment> rows)
        : base(rows.Cast<Fragment>())
    {
        _rows = rows;
    }

    public int DerivedColumnCount { get; init; }

    public IReadOnlyList<TableRowFragment> Rows => _rows;

    protected override void OnChildAdded(Fragment child)
    {
        if (child is TableRowFragment row)
        {
            _rows.Add(row);
        }
    }

    private static List<TableRowFragment> MaterializeRows(IEnumerable<TableRowFragment>? rows)
    {
        return rows?.ToList() ?? [];
    }
}
