using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Pagination;


/// <summary>
/// Final layout plus pagination audit facts.
/// </summary>
internal sealed class PaginationResult
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
