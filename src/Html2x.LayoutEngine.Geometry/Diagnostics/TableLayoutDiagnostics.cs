using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;

namespace Html2x.LayoutEngine.Geometry.Diagnostics;


internal static class TableLayoutDiagnostics
{
    public static void EmitSupportedTable(
        string nodePath,
        int rowCount,
        int derivedColumnCount,
        float? requestedWidth,
        float? resolvedWidth,
        IReadOnlyList<TableRowDiagnosticContext>? rowContexts = null,
        IReadOnlyList<TableCellDiagnosticContext>? cellContexts = null,
        IReadOnlyList<TableColumnDiagnosticContext>? columnContexts = null,
        IReadOnlyList<TableGroupDiagnosticContext>? groupContexts = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var payload = new TableLayoutDiagnostic
        {
            NodePath = nodePath,
            TablePath = nodePath,
            RowCount = rowCount,
            DerivedColumnCount = derivedColumnCount,
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            Outcome = TableLayoutDiagnosticNames.Outcomes.Supported,
            RowContexts = rowContexts ?? [],
            CellContexts = cellContexts ?? [],
            ColumnContexts = columnContexts ?? [],
            GroupContexts = groupContexts ?? []
        };

        EmitTable(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            TableLayoutDiagnosticNames.Events.Table,
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
        IReadOnlyList<TableRowDiagnosticContext>? rowContexts = null,
        IReadOnlyList<TableCellDiagnosticContext>? cellContexts = null,
        IReadOnlyList<TableColumnDiagnosticContext>? columnContexts = null,
        IReadOnlyList<TableGroupDiagnosticContext>? groupContexts = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        var unsupportedPayload = new UnsupportedStructureDiagnostic
        {
            NodePath = nodePath,
            StructureKind = structureKind,
            Reason = reason,
            FormattingContext = formattingContext
        };
        var tablePayload = new TableLayoutDiagnostic
        {
            NodePath = nodePath,
            TablePath = nodePath,
            RowCount = rowCount,
            DerivedColumnCount = null,
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            Outcome = TableLayoutDiagnosticNames.Outcomes.Unsupported,
            Reason = reason,
            RowContexts = rowContexts ?? [],
            CellContexts = cellContexts ?? [],
            ColumnContexts = columnContexts ?? [],
            GroupContexts = groupContexts ?? []
        };

        EmitUnsupportedStructure(
            diagnosticsSink,
            TableLayoutDiagnosticNames.Events.UnsupportedStructure,
            unsupportedPayload);

        EmitTable(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            TableLayoutDiagnosticNames.Events.Table,
            tablePayload);
    }

    private static void EmitTable(
        IDiagnosticsSink? diagnosticsSink,
        DiagnosticSeverity severity,
        string eventName,
        TableLayoutDiagnostic table)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: GeometryDiagnosticNames.Stages.BoxTree,
            Name: eventName,
            Severity: severity,
            Message: table.Reason,
            Context: null,
            Fields: MapTableFields(table),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static void EmitUnsupportedStructure(
        IDiagnosticsSink? diagnosticsSink,
        string eventName,
        UnsupportedStructureDiagnostic unsupported)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: GeometryDiagnosticNames.Stages.BoxTree,
            Name: eventName,
            Severity: DiagnosticSeverity.Error,
            Message: unsupported.Reason,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, unsupported.NodePath),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.StructureKind, unsupported.StructureKind),
                DiagnosticFields.Field(GeometryDiagnosticNames.Fields.Reason, unsupported.Reason),
                DiagnosticFields.Field(
                    GeometryDiagnosticNames.Fields.FormattingContext,
                    DiagnosticValue.FromEnum(unsupported.FormattingContext))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static DiagnosticFields MapTableFields(TableLayoutDiagnostic table)
    {
        return DiagnosticFields.Create(
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.NodePath, table.NodePath),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.TablePath, table.TablePath),
            DiagnosticFields.Field(GeometryDiagnosticNames.Fields.RowCount, table.RowCount),
            DiagnosticFields.Field(
                TableLayoutDiagnosticNames.Fields.DerivedColumnCount,
                FromNullable(table.DerivedColumnCount)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.RequestedWidth, FromNullable(table.RequestedWidth)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.ResolvedWidth, FromNullable(table.ResolvedWidth)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.Outcome, table.Outcome),
            DiagnosticFields.Field(
                GeometryDiagnosticNames.Fields.Reason,
                table.Reason is null ? null : DiagnosticValue.From(table.Reason)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.Rows, MapRows(table.RowContexts)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.Cells, MapCells(table.CellContexts)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.Columns, MapColumns(table.ColumnContexts)),
            DiagnosticFields.Field(TableLayoutDiagnosticNames.Fields.Groups, MapGroups(table.GroupContexts)));
    }

    private static DiagnosticArray MapRows(IReadOnlyList<TableRowDiagnosticContext> rows) =>
        new(rows.Select(static row => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowIndex, row.RowIndex),
            DiagnosticObject.Field(TableLayoutDiagnosticNames.Fields.CellCount, row.CellCount),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Height, row.Height))));

    private static DiagnosticArray MapCells(IReadOnlyList<TableCellDiagnosticContext> cells) =>
        new(cells.Select(static cell => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowIndex, cell.RowIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.ColumnIndex, cell.ColumnIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.IsHeader, cell.IsHeader),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Width, cell.Width),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Height, cell.Height))));

    private static DiagnosticArray MapColumns(IReadOnlyList<TableColumnDiagnosticContext> columns) =>
        new(columns.Select(static column => DiagnosticObject.Create(
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.ColumnIndex, column.ColumnIndex),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.Width, column.Width))));

    private static DiagnosticArray MapGroups(IReadOnlyList<TableGroupDiagnosticContext> groups) =>
        new(groups.Select(static group => DiagnosticObject.Create(
            DiagnosticObject.Field(TableLayoutDiagnosticNames.Fields.GroupKind, group.GroupKind),
            DiagnosticObject.Field(GeometryDiagnosticNames.Fields.RowCount, group.RowCount))));

    private static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private sealed class TableLayoutDiagnostic
    {
        public string NodePath { get; init; } = string.Empty;
        public string TablePath { get; init; } = string.Empty;
        public int RowCount { get; init; }
        public int? DerivedColumnCount { get; init; }
        public float? RequestedWidth { get; init; }
        public float? ResolvedWidth { get; init; }
        public string Outcome { get; init; } = string.Empty;
        public string? Reason { get; init; }
        public IReadOnlyList<TableRowDiagnosticContext> RowContexts { get; init; } = [];
        public IReadOnlyList<TableCellDiagnosticContext> CellContexts { get; init; } = [];
        public IReadOnlyList<TableColumnDiagnosticContext> ColumnContexts { get; init; } = [];
        public IReadOnlyList<TableGroupDiagnosticContext> GroupContexts { get; init; } = [];
    }

    private sealed class UnsupportedStructureDiagnostic
    {
        public string NodePath { get; init; } = string.Empty;
        public string StructureKind { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public FormattingContextKind FormattingContext { get; init; } = FormattingContextKind.Block;
    }
}
