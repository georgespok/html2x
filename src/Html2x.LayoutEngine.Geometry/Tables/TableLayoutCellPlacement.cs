using Html2x.LayoutEngine.Geometry.Models;

namespace Html2x.LayoutEngine.Geometry.Tables;

internal sealed record TableLayoutCellPlacement(
    TableCellBox SourceCell,
    int ColumnIndex,
    int ColumnSpan,
    bool IsHeader,
    UsedGeometry UsedGeometry);
