using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Pagination;


/// <summary>
/// Input facts required by the pagination module.
/// </summary>
internal sealed class PaginationOptions
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
