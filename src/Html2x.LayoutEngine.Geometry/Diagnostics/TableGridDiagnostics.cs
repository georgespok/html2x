using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal static class TableGridDiagnostics
{
    public static void EmitSupportedTable(
        string nodePath,
        int rowCount,
        int derivedColumnCount,
        float? requestedWidth,
        float? resolvedWidth,
        IReadOnlyList<TableRowDiagnosticFacts>? rowFacts = null,
        IReadOnlyList<TableCellDiagnosticFacts>? cellFacts = null,
        IReadOnlyList<TableColumnDiagnosticFacts>? columnFacts = null,
        IReadOnlyList<TableGroupDiagnosticFacts>? groupFacts = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var payload = new TableGridDiagnosticPayload
        {
            NodePath = nodePath,
            TablePath = nodePath,
            RowCount = rowCount,
            DerivedColumnCount = derivedColumnCount,
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            Outcome = TableGridDiagnosticNames.Outcomes.Supported,
            RowFacts = rowFacts ?? [],
            CellFacts = cellFacts ?? [],
            ColumnFacts = columnFacts ?? [],
            GroupFacts = groupFacts ?? []
        };

        EmitTable(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            TableGridDiagnosticNames.Events.Table,
            payload);
    }

    public static void EmitUnsupportedTable(
        string nodePath,
        string structureKind,
        string reason,
        int rowCount = 0,
        float? requestedWidth = null,
        float? resolvedWidth = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block,
        IReadOnlyList<TableRowDiagnosticFacts>? rowFacts = null,
        IReadOnlyList<TableCellDiagnosticFacts>? cellFacts = null,
        IReadOnlyList<TableColumnDiagnosticFacts>? columnFacts = null,
        IReadOnlyList<TableGroupDiagnosticFacts>? groupFacts = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var unsupportedPayload = new UnsupportedStructureDiagnostic
        {
            NodePath = nodePath,
            StructureKind = structureKind,
            Reason = reason,
            FormattingContext = formattingContext
        };
        var tablePayload = new TableGridDiagnosticPayload
        {
            NodePath = nodePath,
            TablePath = nodePath,
            RowCount = rowCount,
            DerivedColumnCount = null,
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            Outcome = TableGridDiagnosticNames.Outcomes.Unsupported,
            Reason = reason,
            RowFacts = rowFacts ?? [],
            CellFacts = cellFacts ?? [],
            ColumnFacts = columnFacts ?? [],
            GroupFacts = groupFacts ?? []
        };

        EmitUnsupportedStructure(
            diagnosticsSink,
            TableGridDiagnosticNames.Events.UnsupportedStructure,
            unsupportedPayload);

        EmitTable(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            TableGridDiagnosticNames.Events.Table,
            tablePayload);
    }

    private static void EmitTable(
        IDiagnosticsSink? diagnosticsSink,
        DiagnosticSeverity severity,
        string eventName,
        TableGridDiagnosticPayload table)
    {
        diagnosticsSink?.Emit(new(
            GeometryDiagnosticNames.Stages.BoxTree,
            eventName,
            severity,
            table.Reason,
            null,
            MapTableFields(table),
            DateTimeOffset.UtcNow));
    }

    private static void EmitUnsupportedStructure(
        IDiagnosticsSink? diagnosticsSink,
        string eventName,
        UnsupportedStructureDiagnostic unsupported)
    {
        diagnosticsSink?.Emit(new(
            GeometryDiagnosticNames.Stages.BoxTree,
            eventName,
            DiagnosticSeverity.Error,
            unsupported.Reason,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, unsupported.NodePath),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.StructureKind, unsupported.StructureKind),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.Reason, unsupported.Reason),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(unsupported.FormattingContext))),
            DateTimeOffset.UtcNow));
    }

    private static DiagnosticFields MapTableFields(TableGridDiagnosticPayload table) =>
        DiagnosticFields.Create(
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, table.NodePath),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.TablePath, table.TablePath),
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.RowCount, table.RowCount),
            DiagnosticFields.Field(
                TableGridDiagnosticNames.Fields.DerivedColumnCount,
                FromNullable(table.DerivedColumnCount)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.RequestedWidth, FromNullable(table.RequestedWidth)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.ResolvedWidth, FromNullable(table.ResolvedWidth)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.Outcome, table.Outcome),
            DiagnosticFields.Field(
                GeometryDiagnosticNames.Fields.Reason,
                table.Reason is null ? null : DiagnosticValue.From(table.Reason)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.Rows, MapRows(table.RowFacts)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.Cells, MapCells(table.CellFacts)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.Columns, MapColumns(table.ColumnFacts)),
            DiagnosticFields.Field(TableGridDiagnosticNames.Fields.Groups, MapGroups(table.GroupFacts)));

    private static DiagnosticArray MapRows(IReadOnlyList<TableRowDiagnosticFacts> rows) =>
        new(rows.Select(static row => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowIndex, row.RowIndex),
            DiagnosticObject.Field(TableGridDiagnosticNames.Fields.CellCount, row.CellCount),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Height, row.Height))));

    private static DiagnosticArray MapCells(IReadOnlyList<TableCellDiagnosticFacts> cells) =>
        new(cells.Select(static cell => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowIndex, cell.RowIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.ColumnIndex, cell.ColumnIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.IsHeader, cell.IsHeader),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Width, cell.Width),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Height, cell.Height))));

    private static DiagnosticArray MapColumns(IReadOnlyList<TableColumnDiagnosticFacts> columns) =>
        new(columns.Select(static column => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.ColumnIndex, column.ColumnIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Width, column.Width))));

    private static DiagnosticArray MapGroups(IReadOnlyList<TableGroupDiagnosticFacts> groups) =>
        new(groups.Select(static group => DiagnosticObject.Create(
            DiagnosticObject.Field(TableGridDiagnosticNames.Fields.GroupKind, group.GroupKind),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowCount, group.RowCount))));

    private static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private sealed class TableGridDiagnosticPayload
    {
        public string NodePath { get; init; } = string.Empty;
        public string TablePath { get; init; } = string.Empty;
        public int RowCount { get; init; }
        public int? DerivedColumnCount { get; init; }
        public float? RequestedWidth { get; init; }
        public float? ResolvedWidth { get; init; }
        public string Outcome { get; init; } = string.Empty;
        public string? Reason { get; init; }
        public IReadOnlyList<TableRowDiagnosticFacts> RowFacts { get; init; } = [];
        public IReadOnlyList<TableCellDiagnosticFacts> CellFacts { get; init; } = [];
        public IReadOnlyList<TableColumnDiagnosticFacts> ColumnFacts { get; init; } = [];
        public IReadOnlyList<TableGroupDiagnosticFacts> GroupFacts { get; init; } = [];
    }

    private sealed class UnsupportedStructureDiagnostic
    {
        public string NodePath { get; init; } = string.Empty;
        public string StructureKind { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public FormattingContextKind FormattingContext { get; init; } = FormattingContextKind.Block;
    }
}