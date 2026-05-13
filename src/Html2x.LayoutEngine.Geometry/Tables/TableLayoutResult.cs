namespace Html2x.LayoutEngine.Geometry.Tables;

internal sealed class TableLayoutResult
{
    public bool IsSupported { get; init; } = true;

    public string? UnsupportedStructureKind { get; init; }

    public string? UnsupportedReason { get; init; }

    public float? RequestedContentWidth { get; init; }

    public float ResolvedBorderBoxWidth { get; init; }

    public int RowCount { get; init; }

    public int DerivedColumnCount { get; init; }

    public IReadOnlyList<float> ColumnWidths { get; init; } = [];

    public IReadOnlyList<TableLayoutRowResult> Rows { get; init; } = [];

    public float ContentHeight { get; init; }

    public float BorderBoxHeight { get; init; }

    public static TableLayoutResult Unsupported(
        float? requestedContentWidth,
        float resolvedBorderBoxWidth,
        string structureKind,
        string reason,
        int rowCount = 0) =>
        new()
        {
            IsSupported = false,
            RequestedContentWidth = requestedContentWidth,
            ResolvedBorderBoxWidth = resolvedBorderBoxWidth,
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
