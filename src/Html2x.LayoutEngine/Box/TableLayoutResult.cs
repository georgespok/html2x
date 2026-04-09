using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public sealed class TableLayoutResult
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

    public float Height { get; init; }

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
            Height = 0f
        };
    }
}

public sealed class TableLayoutRowResult
{
    public required TableRowBox SourceRow { get; init; }

    public int RowIndex { get; init; }

    public float Y { get; init; }

    public IReadOnlyList<TableLayoutCellPlacement> Cells { get; init; } = [];

    public float Height { get; init; }
}

public sealed class TableLayoutCellPlacement
{
    public required TableCellBox SourceCell { get; init; }

    public int ColumnIndex { get; init; }

    public bool IsHeader { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public float Width { get; init; }

    public float Height { get; init; }
}
