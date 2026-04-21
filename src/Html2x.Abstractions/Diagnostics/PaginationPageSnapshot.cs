using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

public sealed class PaginationPageSnapshot
{
    public int PageNumber { get; init; }

    public SizePt PageSize { get; init; }

    public Spacing Margin { get; init; }

    public float ContentTop { get; init; }

    public float ContentBottom { get; init; }

    public IReadOnlyList<PaginationPlacementSnapshot> Placements { get; init; } = [];
}
