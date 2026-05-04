using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Pagination;


internal sealed class BlockPaginationPage
{
    public required int PageNumber { get; init; }

    public required SizePt PageSize { get; init; }

    public required Spacing Margin { get; init; }

    public required RectPt ContentArea { get; init; }

    public required IReadOnlyList<BlockFragmentPlacement> Placements { get; init; }
}
