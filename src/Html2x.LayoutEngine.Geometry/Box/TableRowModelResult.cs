namespace Html2x.LayoutEngine.Geometry.Box;


internal readonly record struct TableRowModelResult(
    bool IsSupported,
    string UnsupportedStructureKind,
    string UnsupportedReason,
    IReadOnlyList<TableRowBox> Rows,
    int RowCount)
{
    public static TableRowModelResult Supported(IReadOnlyList<TableRowBox> rows, int rowCount)
    {
        return new TableRowModelResult(
            true,
            string.Empty,
            string.Empty,
            rows,
            rowCount);
    }

    public static TableRowModelResult Unsupported(string structureKind, string reason, int rowCount)
    {
        return new TableRowModelResult(
            false,
            structureKind,
            reason,
            [],
            rowCount);
    }
}
