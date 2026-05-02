using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;

internal sealed class LayoutSnapshot
{
    public int PageCount { get; init; }

    public IReadOnlyList<LayoutPageSnapshot> Pages { get; init; } = [];
}

internal sealed class LayoutPageSnapshot
{
    public int PageNumber { get; init; }

    public SizePt PageSize { get; init; }

    public Spacing Margin { get; init; }

    public IReadOnlyList<FragmentSnapshot> Fragments { get; init; } = [];
}

internal sealed class FragmentSnapshot
{
    public int SequenceId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }

    public ColorRgba? Color { get; init; }

    public ColorRgba? BackgroundColor { get; init; }

    public Spacing? Margin { get; init; }

    public Spacing? Padding { get; init; }

    public float? WidthPt { get; init; }

    public float? HeightPt { get; init; }

    public string? Display { get; init; }

    public string? Text { get; init; }

    public float? ContentX { get; init; }

    public float? ContentY { get; init; }

    public SizePt? ContentSize { get; init; }

    public float? OccupiedX { get; init; }

    public float? OccupiedY { get; init; }

    public SizePt? OccupiedSize { get; init; }

    public BorderEdges? Borders { get; init; }

    public FragmentDisplayRole? DisplayRole { get; init; }

    public FormattingContextKind? FormattingContext { get; init; }

    public float? MarkerOffset { get; init; }

    public int? DerivedColumnCount { get; init; }

    public int? RowIndex { get; init; }

    public int? ColumnIndex { get; init; }

    public bool? IsHeader { get; init; }

    public string? MetadataOwner { get; init; }

    public string? MetadataConsumer { get; init; }

    public IReadOnlyList<FragmentSnapshot> Children { get; init; } = [];
}

internal sealed class GeometrySnapshot
{
    public LayoutSnapshot Fragments { get; init; } = new();

    public IReadOnlyList<BoxGeometrySnapshot> Boxes { get; init; } = [];

    public IReadOnlyList<PaginationPageSnapshot> Pagination { get; init; } = [];
}

internal sealed class BoxGeometrySnapshot
{
    public int SequenceId { get; init; }

    public string Path { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? TagName { get; init; }

    public int? SourceNodeId { get; init; }

    public int? SourceContentId { get; init; }

    public string? SourcePath { get; init; }

    public int? SourceOrder { get; init; }

    public string? SourceElementIdentity { get; init; }

    public string? GeneratedSourceKind { get; init; }

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

    public string? MetadataOwner { get; init; }

    public string? MetadataConsumer { get; init; }

    public IReadOnlyList<BoxGeometrySnapshot> Children { get; init; } = [];
}

internal sealed class PaginationPageSnapshot
{
    public int PageNumber { get; init; }

    public SizePt PageSize { get; init; }

    public Spacing Margin { get; init; }

    public float ContentTop { get; init; }

    public float ContentBottom { get; init; }

    public IReadOnlyList<PaginationPlacementSnapshot> Placements { get; init; } = [];
}

internal sealed class PaginationPlacementSnapshot
{
    public int FragmentId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public int PageNumber { get; init; }

    public int OrderIndex { get; init; }

    public bool IsOversized { get; init; }

    public PaginationDecisionKind DecisionKind { get; init; }

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
