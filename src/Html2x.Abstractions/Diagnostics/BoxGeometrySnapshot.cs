using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

public sealed class BoxGeometrySnapshot
{
    public int SequenceId { get; init; }

    public string Path { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? TagName { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public float? ContentX { get; init; }

    public float? ContentY { get; init; }

    public SizePt? ContentSize { get; init; }

    public float? Baseline { get; init; }

    public float MarkerOffset { get; init; }

    public bool AllowsOverflow { get; init; }

    public bool IsAnonymous { get; init; }

    public bool IsInlineBlockContext { get; init; }

    public int? DerivedColumnCount { get; init; }

    public int? RowIndex { get; init; }

    public int? ColumnIndex { get; init; }

    public bool? IsHeader { get; init; }

    public IReadOnlyList<BoxGeometrySnapshot> Children { get; init; } = [];
}
