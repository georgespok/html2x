namespace Html2x.LayoutEngine.Geometry.Box;


internal sealed record TableLayoutCellPlacement(
    TableCellBox SourceCell,
    int ColumnIndex,
    bool IsHeader,
    UsedGeometry UsedGeometry);
