using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


internal sealed record TableLayoutRowResult(
    TableRowBox SourceRow,
    int RowIndex,
    UsedGeometry UsedGeometry,
    IReadOnlyList<TableLayoutCellPlacement> Cells);
