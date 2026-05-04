using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed class PaginationPageSnapshot
{
    public int PageNumber { get; init; }

    public SizePt PageSize { get; init; }

    public Spacing Margin { get; init; }

    public float ContentTop { get; init; }

    public float ContentBottom { get; init; }

    public IReadOnlyList<PaginationPlacementSnapshot> Placements { get; init; } = [];
}
