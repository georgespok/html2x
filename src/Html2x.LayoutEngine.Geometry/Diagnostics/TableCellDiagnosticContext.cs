namespace Html2x.LayoutEngine.Geometry.Diagnostics;


internal sealed record TableCellDiagnosticContext(
    int RowIndex,
    int ColumnIndex,
    bool IsHeader,
    float Width,
    float Height);
