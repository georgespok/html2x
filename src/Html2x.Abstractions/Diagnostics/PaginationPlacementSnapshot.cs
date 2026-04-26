using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.Abstractions.Diagnostics;

public sealed class PaginationPlacementSnapshot
{
    public int FragmentId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public int PageNumber { get; init; }

    public int OrderIndex { get; init; }

    public bool IsOversized { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public FragmentDisplayRole? DisplayRole { get; init; }

    public FormattingContextKind? FormattingContext { get; init; }

    public float? MarkerOffset { get; init; }

    public int? DerivedColumnCount { get; init; }

    public int? RowIndex { get; init; }

    public int? ColumnIndex { get; init; }

    public bool? IsHeader { get; init; }

    public string? MetadataOwner { get; init; }

    public string? MetadataConsumer { get; init; }
}
