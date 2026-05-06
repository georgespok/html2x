namespace Html2x.LayoutEngine.Geometry.Diagnostics;

internal sealed record TableCellDiagnosticFacts(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height);