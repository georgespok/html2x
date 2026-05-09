using Html2x.LayoutEngine.Geometry.Models;

namespace Html2x.LayoutEngine.Geometry.Tables;

internal readonly record struct TableStructureResult(
    bool IsSupported,
    string UnsupportedStructureKind,
    string UnsupportedReason,
    IReadOnlyList<TableRowFacts> Rows,
    int RowCount)
{
    public static TableStructureResult Supported(IReadOnlyList<TableRowFacts> rows, int rowCount) =>
        new(
            true,
            string.Empty,
            string.Empty,
            rows,
            rowCount);

    public static TableStructureResult Unsupported(string structureKind, string reason, int rowCount) =>
        new(
            false,
            structureKind,
            reason,
            [],
            rowCount);
}

internal sealed record TableRowFacts(
    TableRowBox SourceRow,
    IReadOnlyList<TableCellFacts> Cells,
    int EffectiveColumnCount);

internal sealed record TableCellFacts(
    TableCellBox SourceCell,
    int ColumnSpan);
