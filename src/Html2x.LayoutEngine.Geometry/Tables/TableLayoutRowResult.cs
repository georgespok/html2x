using Html2x.LayoutEngine.Geometry.Models;

namespace Html2x.LayoutEngine.Geometry.Tables;

internal sealed record TableLayoutRowResult(
    TableRowBox SourceRow,
    int RowIndex,
    UsedGeometry UsedGeometry,
    IReadOnlyList<TableLayoutCellPlacement> Cells);
