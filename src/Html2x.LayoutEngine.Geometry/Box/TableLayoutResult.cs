namespace Html2x.LayoutEngine.Geometry.Box;


internal sealed class TableLayoutResult
{
    public bool IsSupported { get; init; } = true;

    public string? UnsupportedStructureKind { get; init; }

    public string? UnsupportedReason { get; init; }

    public float? RequestedWidth { get; init; }

    public float ResolvedWidth { get; init; }

    public int RowCount { get; init; }

    public int DerivedColumnCount { get; init; }

    public IReadOnlyList<float> ColumnWidths { get; init; } = [];

    public IReadOnlyList<TableLayoutRowResult> Rows { get; init; } = [];

    public float ContentHeight { get; init; }

    public float BorderBoxHeight { get; init; }

    public static TableLayoutResult Unsupported(
        float? requestedWidth,
        float resolvedWidth,
        string structureKind,
        string reason,
        int rowCount = 0)
    {
        return new TableLayoutResult
        {
            IsSupported = false,
            RequestedWidth = requestedWidth,
            ResolvedWidth = resolvedWidth,
            RowCount = rowCount,
            UnsupportedStructureKind = structureKind,
            UnsupportedReason = reason,
            DerivedColumnCount = 0,
            ColumnWidths = [],
            Rows = [],
            ContentHeight = 0f,
            BorderBoxHeight = 0f
        };
    }
}
