using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Pagination;


/// <summary>
/// Audit facts for one fragment placement.
/// </summary>
internal sealed class PaginationPlacementAudit
{
    /// <summary>
    /// Gets the placed fragment identifier.
    /// </summary>
    public required int FragmentId { get; init; }

    /// <summary>
    /// Gets the one-based page number where the fragment was placed.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the page-local placed rectangle.
    /// </summary>
    public required RectPt PlacedRect { get; init; }

    /// <summary>
    /// Gets the zero-based source order index used by pagination.
    /// </summary>
    public required int OrderIndex { get; init; }

    /// <summary>
    /// Gets the pagination decision for this placement.
    /// </summary>
    public required PaginationDecisionKind DecisionKind { get; init; }

    /// <summary>
    /// Gets a value indicating whether this fragment exceeds the page content height.
    /// </summary>
    public required bool IsOversized { get; init; }

    /// <summary>
    /// Gets a stable fragment kind label for diagnostics.
    /// </summary>
    public required string FragmentKind { get; init; }

    /// <summary>
    /// Gets the display role metadata copied from the placed fragment, when present.
    /// </summary>
    public FragmentDisplayRole? DisplayRole { get; init; }

    /// <summary>
    /// Gets the formatting context metadata copied from the placed fragment, when present.
    /// </summary>
    public FormattingContextKind? FormattingContext { get; init; }

    /// <summary>
    /// Gets the marker offset metadata copied from the placed fragment, when present.
    /// </summary>
    public float? MarkerOffset { get; init; }

    /// <summary>
    /// Gets the derived table column count copied from the placed fragment, when present.
    /// </summary>
    public int? DerivedColumnCount { get; init; }

    /// <summary>
    /// Gets the table row index copied from the placed fragment, when present.
    /// </summary>
    public int? RowIndex { get; init; }

    /// <summary>
    /// Gets the table column index copied from the placed fragment, when present.
    /// </summary>
    public int? ColumnIndex { get; init; }

    /// <summary>
    /// Gets the table header flag copied from the placed fragment, when present.
    /// </summary>
    public bool? IsHeader { get; init; }

    /// <summary>
    /// Gets the placed rectangle x coordinate.
    /// </summary>
    public float PageX => PlacedRect.X;

    /// <summary>
    /// Gets the placed rectangle y coordinate.
    /// </summary>
    public float PageY => PlacedRect.Y;

    /// <summary>
    /// Gets the placed rectangle width.
    /// </summary>
    public float Width => PlacedRect.Width;

    /// <summary>
    /// Gets the placed rectangle height.
    /// </summary>
    public float Height => PlacedRect.Height;
}
