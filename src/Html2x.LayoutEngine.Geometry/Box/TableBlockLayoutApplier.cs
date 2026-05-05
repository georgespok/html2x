using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Applies table layout, table diagnostics, and table placement to table boxes.
/// </summary>
internal sealed class TableBlockLayoutApplier(
    TableGridLayout tableGridLayout,
    TablePlacementApplier tablePlacementApplier,
    IDiagnosticsSink? diagnosticsSink)
{
    private readonly TableGridLayout _tableGridLayout =
        tableGridLayout ?? throw new ArgumentNullException(nameof(tableGridLayout));
    private readonly TablePlacementApplier _tablePlacementApplier =
        tablePlacementApplier ?? throw new ArgumentNullException(nameof(tablePlacementApplier));

    public void Apply(
        TableBox node,
        BlockLayoutRequest request,
        Func<BlockBox, float, float, float, float, float> layoutChildBlocks)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(layoutChildBlocks);

        var margin = node.Style.Margin.Safe();
        var origin = BlockOriginResolver.ResolveOrigin(request, margin);
        var result = _tableGridLayout.Layout(node, request.ContentWidth);
        if (!result.IsSupported)
        {
            TableLayoutDiagnostics.EmitUnsupportedTable(
                BoxNodePathBuilder.Build(node),
                result.UnsupportedStructureKind ?? TableStructureDiagnosticNames.StructureKinds.UnsupportedTableStructure,
                result.UnsupportedReason ?? TableStructureDiagnosticNames.Reasons.UnsupportedTableStructure,
                result.RowCount,
                result.RequestedWidth,
                result.ResolvedWidth,
                groupContexts: BuildTableGroupContexts(node),
                diagnosticsSink: diagnosticsSink);

            _tablePlacementApplier.ApplyUnsupportedPlaceholder(node, origin.X, origin.Y, result.ResolvedWidth, margin);
            return;
        }

        TableLayoutDiagnostics.EmitSupportedTable(
            BoxNodePathBuilder.Build(node),
            result.Rows.Count,
            result.DerivedColumnCount,
            result.RequestedWidth,
            result.ResolvedWidth,
            BuildTableRowContexts(result),
            BuildTableCellContexts(result),
            BuildTableColumnContexts(result),
            BuildTableGroupContexts(node),
            diagnosticsSink: diagnosticsSink);

        _tablePlacementApplier.ApplySupported(node, result, origin.X, origin.Y, margin, layoutChildBlocks);
    }

    private static IReadOnlyList<TableRowDiagnosticContext> BuildTableRowContexts(TableLayoutResult result)
    {
        return result.Rows
            .Select(static row => new TableRowDiagnosticContext(
                row.RowIndex,
                row.Cells.Count,
                row.UsedGeometry.Height))
            .ToList();
    }

    private static IReadOnlyList<TableCellDiagnosticContext> BuildTableCellContexts(TableLayoutResult result)
    {
        return result.Rows
            .SelectMany(static row => row.Cells.Select(cell => new TableCellDiagnosticContext(
                row.RowIndex,
                cell.ColumnIndex,
                cell.IsHeader,
                cell.UsedGeometry.Width,
                cell.UsedGeometry.Height)))
            .ToList();
    }

    private static IReadOnlyList<TableColumnDiagnosticContext> BuildTableColumnContexts(TableLayoutResult result)
    {
        return result.ColumnWidths
            .Select(static (width, index) => new TableColumnDiagnosticContext(index, width))
            .ToList();
    }

    private static IReadOnlyList<TableGroupDiagnosticContext> BuildTableGroupContexts(TableBox table)
    {
        var groups = table.Children
            .OfType<TableSectionBox>()
            .Select(static section => new TableGroupDiagnosticContext(
                section.Element?.TagName.ToLowerInvariant() ?? TableLayoutDiagnosticNames.GroupKinds.Section,
                section.Children.OfType<TableRowBox>().Count()))
            .ToList();

        if (groups.Count > 0)
        {
            return groups;
        }

        return
        [
            new TableGroupDiagnosticContext(
                TableLayoutDiagnosticNames.GroupKinds.Direct,
                table.Children.OfType<TableRowBox>().Count())
        ];
    }

}
