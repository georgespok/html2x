using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

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