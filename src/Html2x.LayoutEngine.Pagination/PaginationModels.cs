using System.Drawing;
using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Input facts required by the pagination module.
/// </summary>
public sealed class PaginationOptions
{
    /// <summary>
    /// Gets the target page size.
    /// </summary>
    public required SizePt PageSize { get; init; }

    /// <summary>
    /// Gets the page margin used to resolve the content area.
    /// </summary>
    public required Spacing Margin { get; init; }
}

/// <summary>
/// Stable vocabulary for pagination placement decisions.
/// </summary>
public enum PaginationDecisionKind
{
    /// <summary>
    /// The fragment stayed on the current page.
    /// </summary>
    Placed,

    /// <summary>
    /// The fragment moved as a whole to the next page.
    /// </summary>
    MovedToNextPage,

    /// <summary>
    /// The fragment is taller than the available page content height.
    /// </summary>
    Oversized,

    /// <summary>
    /// Reserved for future support that splits one fragment across pages.
    /// </summary>
    SplitAcrossPages,

    /// <summary>
    /// Reserved for future explicit page break support.
    /// </summary>
    ForcedBreak
}

/// <summary>
/// Audit facts for one paginated page.
/// </summary>
public sealed class PaginationPageAudit
{
    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the target page size used for this page.
    /// </summary>
    public required SizePt PageSize { get; init; }

    /// <summary>
    /// Gets the page margin used for this page.
    /// </summary>
    public required Spacing Margin { get; init; }

    /// <summary>
    /// Gets the normalized content area inside the page margin.
    /// </summary>
    public required RectangleF ContentArea { get; init; }

    /// <summary>
    /// Gets the placement audit facts for fragments on this page.
    /// </summary>
    public required IReadOnlyList<PaginationPlacementAudit> Placements { get; init; }

    /// <summary>
    /// Gets the top edge of the page content area.
    /// </summary>
    public float ContentTop => ContentArea.Y;

    /// <summary>
    /// Gets the bottom edge of the page content area.
    /// </summary>
    public float ContentBottom => ContentArea.Bottom;
}

/// <summary>
/// Audit facts for one fragment placement.
/// </summary>
public sealed class PaginationPlacementAudit
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
    public required RectangleF PlacedRect { get; init; }

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

/// <summary>
/// Final layout plus pagination audit facts.
/// </summary>
public sealed class PaginationResult
{
    /// <summary>
    /// Gets the final paginated layout.
    /// </summary>
    public required HtmlLayout Layout { get; init; }

    /// <summary>
    /// Gets stable page and placement audit facts.
    /// </summary>
    public required IReadOnlyList<PaginationPageAudit> AuditPages { get; init; }

    /// <summary>
    /// Gets the number of audit pages.
    /// </summary>
    public int TotalPages => AuditPages.Count;

    /// <summary>
    /// Gets the total number of audited placements across all pages.
    /// </summary>
    public int TotalPlacements => AuditPages.Sum(static page => page.Placements.Count);
}

internal sealed class BlockPaginationPlan
{
    public required IReadOnlyList<BlockPaginationPage> Pages { get; init; }
}

internal sealed class BlockPaginationPage
{
    public required int PageNumber { get; init; }

    public required SizePt PageSize { get; init; }

    public required Spacing Margin { get; init; }

    public required RectangleF ContentArea { get; init; }

    public required IReadOnlyList<BlockFragmentPlacement> Placements { get; init; }
}

internal sealed class BlockFragmentPlacement
{
    public required int FragmentId { get; init; }

    public required int PageNumber { get; init; }

    public required PaginationDecisionKind DecisionKind { get; init; }

    public required bool IsOversized { get; init; }

    public required int OrderIndex { get; init; }

    public required BlockFragment Fragment { get; init; }

    public RectangleF PlacedRect => Fragment.Rect;

    public float PageX => Fragment.Rect.X;

    public float PageY => Fragment.Rect.Y;

    public float Width => Fragment.Rect.Width;

    public float Height => Fragment.Rect.Height;
}
