using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Pagination;


internal sealed class BlockPaginationPage
{
    public required int PageNumber { get; init; }

    public required SizePt PageSize { get; init; }

    public required Spacing Margin { get; init; }

    public required RectPt ContentArea { get; init; }

    public required IReadOnlyList<BlockFragmentPlacement> Placements { get; init; }
}
