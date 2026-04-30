using Html2x.Abstractions.Layout.Fragments;
using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class TableLayoutDiagnostics
{
    private const string SupportedTableEvent = "layout/table";
    private const string UnsupportedTableEvent = "layout/table/unsupported-structure";

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
            Outcome = "Supported",
            RowContexts = rowContexts ?? [],
            CellContexts = cellContexts ?? [],
            ColumnContexts = columnContexts ?? [],
            GroupContexts = groupContexts ?? []
        };

        Emit(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            SupportedTableEvent,
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
            Outcome = "Unsupported",
            Reason = reason,
            RowContexts = rowContexts ?? [],
            CellContexts = cellContexts ?? [],
            ColumnContexts = columnContexts ?? [],
            GroupContexts = groupContexts ?? []
        };

        Emit(
            diagnosticsSink,
            DiagnosticSeverity.Error,
            UnsupportedTableEvent,
            unsupportedPayload);

        Emit(
            diagnosticsSink,
            DiagnosticSeverity.Info,
            SupportedTableEvent,
            tablePayload);
    }

    private static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        DiagnosticSeverity severity,
        string eventName,
        object payload)
    {
        switch (payload)
        {
            case TableLayoutDiagnostic table:
                diagnosticsSink?.Emit(new DiagnosticRecord(
                    Stage: "stage/box-tree",
                    Name: eventName,
                    Severity: severity,
                    Message: table.Reason,
                    Context: null,
                    Fields: MapTableFields(table),
                    Timestamp: DateTimeOffset.UtcNow));
                break;
            case UnsupportedStructureDiagnostic unsupported:
                diagnosticsSink?.Emit(new DiagnosticRecord(
                    Stage: "stage/box-tree",
                    Name: eventName,
                    Severity: DiagnosticSeverity.Error,
                    Message: unsupported.Reason,
                    Context: null,
                    Fields: DiagnosticFields.Create(
                        DiagnosticFields.Field("nodePath", unsupported.NodePath),
                        DiagnosticFields.Field("structureKind", unsupported.StructureKind),
                        DiagnosticFields.Field("reason", unsupported.Reason),
                        DiagnosticFields.Field("formattingContext", DiagnosticValue.FromEnum(unsupported.FormattingContext))),
                    Timestamp: DateTimeOffset.UtcNow));
                break;
        }
    }

    private static DiagnosticFields MapTableFields(TableLayoutDiagnostic table)
    {
        return DiagnosticFields.Create(
            DiagnosticFields.Field("nodePath", table.NodePath),
            DiagnosticFields.Field("tablePath", table.TablePath),
            DiagnosticFields.Field("rowCount", table.RowCount),
            DiagnosticFields.Field("derivedColumnCount", FromNullable(table.DerivedColumnCount)),
            DiagnosticFields.Field("requestedWidth", FromNullable(table.RequestedWidth)),
            DiagnosticFields.Field("resolvedWidth", FromNullable(table.ResolvedWidth)),
            DiagnosticFields.Field("outcome", table.Outcome),
            DiagnosticFields.Field("reason", table.Reason is null ? null : DiagnosticValue.From(table.Reason)),
            DiagnosticFields.Field("rows", MapRows(table.RowContexts)),
            DiagnosticFields.Field("cells", MapCells(table.CellContexts)),
            DiagnosticFields.Field("columns", MapColumns(table.ColumnContexts)),
            DiagnosticFields.Field("groups", MapGroups(table.GroupContexts)));
    }

    private static DiagnosticArray MapRows(IReadOnlyList<TableRowDiagnosticContext> rows) =>
        new(rows.Select(static row => DiagnosticObject.Create(
            DiagnosticObject.Field("rowIndex", row.RowIndex),
            DiagnosticObject.Field("cellCount", row.CellCount),
            DiagnosticObject.Field("height", row.Height))));

    private static DiagnosticArray MapCells(IReadOnlyList<TableCellDiagnosticContext> cells) =>
        new(cells.Select(static cell => DiagnosticObject.Create(
            DiagnosticObject.Field("rowIndex", cell.RowIndex),
            DiagnosticObject.Field("columnIndex", cell.ColumnIndex),
            DiagnosticObject.Field("isHeader", cell.IsHeader),
            DiagnosticObject.Field("width", cell.Width),
            DiagnosticObject.Field("height", cell.Height))));

    private static DiagnosticArray MapColumns(IReadOnlyList<TableColumnDiagnosticContext> columns) =>
        new(columns.Select(static column => DiagnosticObject.Create(
            DiagnosticObject.Field("columnIndex", column.ColumnIndex),
            DiagnosticObject.Field("width", column.Width))));

    private static DiagnosticArray MapGroups(IReadOnlyList<TableGroupDiagnosticContext> groups) =>
        new(groups.Select(static group => DiagnosticObject.Create(
            DiagnosticObject.Field("groupKind", group.GroupKind),
            DiagnosticObject.Field("rowCount", group.RowCount))));

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

internal sealed record TableRowDiagnosticContext(
    int RowIndex,
    int CellCount,
    float Height);

internal sealed record TableCellDiagnosticContext(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height);

internal sealed record TableColumnDiagnosticContext(
    int ColumnIndex,
    float Width);

internal sealed record TableGroupDiagnosticContext(
    string GroupKind,
    int RowCount);
