namespace Html2x.LayoutEngine.Pagination;


/// <summary>
/// Stable vocabulary for pagination placement decisions.
/// </summary>
internal enum PaginationDecisionKind
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
