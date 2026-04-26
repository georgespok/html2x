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

public sealed record TableLayoutRowResult(
    TableRowBox SourceRow,
    int RowIndex,
    UsedGeometry UsedGeometry,
    IReadOnlyList<TableLayoutCellPlacement> Cells)
{
    // Compatibility projection over UsedGeometry. New placement code should consume UsedGeometry directly.
    public float Y => UsedGeometry.Y;

    public float Height => UsedGeometry.Height;
}

public sealed record TableLayoutCellPlacement(
    TableCellBox SourceCell,
    int ColumnIndex,
    bool IsHeader,
    UsedGeometry UsedGeometry)
{
    // Compatibility projection over UsedGeometry. New placement code should consume UsedGeometry directly.
    public float X => UsedGeometry.X;

    public float Y => UsedGeometry.Y;

    public float Width => UsedGeometry.Width;

    public float Height => UsedGeometry.Height;
}
