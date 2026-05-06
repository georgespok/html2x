namespace Html2x.LayoutEngine.Geometry.Box;

internal readonly record struct TableStructureResult(
    bool IsSupported,
    string UnsupportedStructureKind,
    string UnsupportedReason,
    IReadOnlyList<TableRowBox> Rows,
    int RowCount)
{
    public static TableStructureResult Supported(IReadOnlyList<TableRowBox> rows, int rowCount) =>
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