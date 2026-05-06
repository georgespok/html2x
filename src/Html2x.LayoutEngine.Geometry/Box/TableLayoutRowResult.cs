namespace Html2x.LayoutEngine.Geometry.Box;

internal sealed record TableLayoutRowResult(
    TableRowBox SourceRow,
    int RowIndex,
    UsedGeometry UsedGeometry,
    IReadOnlyList<TableLayoutCellPlacement> Cells);