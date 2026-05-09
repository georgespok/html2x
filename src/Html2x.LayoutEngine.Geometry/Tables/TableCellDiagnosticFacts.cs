namespace Html2x.LayoutEngine.Geometry.Tables;

internal sealed record TableCellDiagnosticFacts(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height,
    int ColumnSpan = 1);
