using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

public sealed class PaginationPlacementSnapshot
{
    public int FragmentId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public int PageNumber { get; init; }

    public int OrderIndex { get; init; }

    public bool IsOversized { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public SizePt Size { get; init; }
}
