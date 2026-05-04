using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


internal sealed record TableLayoutCellPlacement(
    TableCellBox SourceCell,
    int ColumnIndex,
    bool IsHeader,
    UsedGeometry UsedGeometry);
