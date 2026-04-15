namespace Html2x.Abstractions.Diagnostics;

public sealed class TableLayoutPayload : IDiagnosticsPayload
{
    public string Kind => "layout.table";

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

public sealed record TableRowDiagnosticContext(
    int RowIndex,
    int CellCount,
    float Height);

public sealed record TableCellDiagnosticContext(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height);

public sealed record TableColumnDiagnosticContext(
    int ColumnIndex,
    float Width);

public sealed record TableGroupDiagnosticContext(
    string GroupKind,
    int RowCount);
