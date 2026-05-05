using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Pagination;


/// <summary>
/// Audit facts for one paginated page.
/// </summary>
internal sealed class PaginationPageAudit
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
    public required RectPt ContentArea { get; init; }

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
