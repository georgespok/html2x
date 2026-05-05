namespace Html2x.RenderModel.Fragments;

/// <summary>
/// Represents a table row fragment with cell children and row metadata.
/// </summary>
public sealed class TableRowFragment : BlockFragment
{
    private readonly List<TableCellFragment> _cells;

    public TableRowFragment()
        : this([])
    {
    }

    public TableRowFragment(IEnumerable<TableCellFragment>? cells)
        : this(MaterializeCells(cells))
    {
    }

    private TableRowFragment(List<TableCellFragment> cells)
        : base(cells)
    {
        _cells = cells;
    }

    public int RowIndex { get; init; }

    public IReadOnlyList<TableCellFragment> Cells => _cells;

    protected override void OnChildAdded(Fragment child)
    {
        if (child is TableCellFragment cell)
        {
            _cells.Add(cell);
        }
    }

    private static List<TableCellFragment> MaterializeCells(IEnumerable<TableCellFragment>? cells)
    {
        return cells?.ToList() ?? [];
    }
}
