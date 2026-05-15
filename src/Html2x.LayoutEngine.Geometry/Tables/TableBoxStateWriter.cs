using Html2x.LayoutEngine.Geometry.Writing;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Tables;

/// <summary>
///     Writes table, row, and cell layout state after table placement resolves geometry facts.
/// </summary>
internal sealed class TableBoxStateWriter(LayoutBoxStateWriter stateWriter)
{
    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public TableBoxStateWriter()
        : this(new())
    {
    }

    public void ApplyTableLayout(
        TableBox table,
        Spacing margin,
        Spacing padding,
        UsedGeometry geometry,
        int derivedColumnCount)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.ApplyTableState(derivedColumnCount);
        _stateWriter.ApplyBlockLayout(table, margin, padding, geometry);
    }

    public void ApplyUnsupportedTablePlaceholder(
        TableBox table,
        Spacing margin,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(table);

        table.ApplyUnsupportedPlaceholderState();
        _stateWriter.ApplyBlockLayout(
            table,
            margin,
            table.Style.Padding.Safe(),
            geometry);
    }

    public void ApplyTableRowLayout(
        TableRowBox row,
        int rowIndex,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(row);

        row.ApplyRowState(rowIndex);
        _stateWriter.ApplyBlockLayout(
            row,
            row.Style.Margin.Safe(),
            row.Style.Padding.Safe(),
            geometry);
    }

    public void ApplyTableCellLayout(
        TableCellBox cell,
        int columnIndex,
        int columnSpan,
        bool isHeader,
        UsedGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(cell);

        cell.ApplyCellState(columnIndex, columnSpan, isHeader);
        _stateWriter.ApplyBlockLayout(
            cell,
            cell.Style.Margin.Safe(),
            cell.Style.Padding.Safe(),
            geometry);
    }
}
