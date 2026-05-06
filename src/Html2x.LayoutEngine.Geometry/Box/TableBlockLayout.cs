using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Coordinates table grid layout, diagnostics, and table placement writing.
/// </summary>
internal sealed class TableBlockLayout(
    TableGridLayout tableGridLayout,
    TablePlacementWriter tablePlacementWriter,
    IDiagnosticsSink? diagnosticsSink)
{
    private readonly TableGridLayout _tableGridLayout =
        tableGridLayout ?? throw new ArgumentNullException(nameof(tableGridLayout));

    private readonly TablePlacementWriter _tablePlacementWriter =
        tablePlacementWriter ?? throw new ArgumentNullException(nameof(tablePlacementWriter));

    public void Layout(
        TableBox node,
        BlockLayoutRequest request,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(layoutChildBlocks);

        var margin = node.Style.Margin.Safe();
        var origin = BlockOriginRules.ResolveOrigin(request, margin);
        var result = _tableGridLayout.Layout(node, request.ContentWidth);
        if (!result.IsSupported)
        {
            TableGridDiagnostics.EmitUnsupportedTable(
                BoxNodePath.Build(node),
                result.UnsupportedStructureKind ??
                TableStructureDiagnosticNames.StructureKinds.UnsupportedTableStructure,
                result.UnsupportedReason ?? TableStructureDiagnosticNames.Reasons.UnsupportedTableStructure,
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupFacts: BuildTableGroupFacts(node),
                diagnosticsSink: diagnosticsSink);

            _tablePlacementWriter.WriteUnsupportedPlaceholder(node, origin.X, origin.Y, result.ResolvedWidth, margin);
            return;
        }

        TableGridDiagnostics.EmitSupportedTable(
            BoxNodePath.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowFacts(result),
            BuildTableCellFacts(result),
            BuildTableColumnFacts(result),
            BuildTableGroupFacts(node),
            diagnosticsSink);

        _tablePlacementWriter.WriteSupported(node, result, origin.X, origin.Y, margin, layoutChildBlocks);
    }

    private static IReadOnlyList<TableRowDiagnosticFacts> BuildTableRowFacts(TableLayoutResult result)
    {
        return result.Rows
            .Select(static row => new TableRowDiagnosticFacts(
                row.RowIndex,
                row.Cells.Count,
                row.UsedGeometry.Height))
            .ToList();
    }

    private static IReadOnlyList<TableCellDiagnosticFacts> BuildTableCellFacts(TableLayoutResult result)
    {
        return result.Rows
            .SelectMany(static row => row.Cells.Select(cell => new TableCellDiagnosticFacts(
                row.RowIndex,
                cell.ColumnIndex,
                cell.IsHeader,
                cell.UsedGeometry.Width,
                cell.UsedGeometry.Height)))
            .ToList();
    }

    private static IReadOnlyList<TableColumnDiagnosticFacts> BuildTableColumnFacts(TableLayoutResult result)
    {
        return result.ColumnWidths
            .Select(static (width, index) => new TableColumnDiagnosticFacts(index, width))
            .ToList();
    }

    private static IReadOnlyList<TableGroupDiagnosticFacts> BuildTableGroupFacts(TableBox table)
    {
        var groups = table.Children
            .OfType<TableSectionBox>()
            .Select(static section => new TableGroupDiagnosticFacts(
                section.Element?.TagName.ToLowerInvariant() ?? TableGridDiagnosticNames.GroupKinds.Section,
                section.Children.OfType<TableRowBox>().Count()))
            .ToList();

        if (groups.Count > 0)
        {
            return groups;
        }

        return
        [
            new(
                TableGridDiagnosticNames.GroupKinds.Direct,
                table.Children.OfType<TableRowBox>().Count())
        ];
    }
}