using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Pagination;

public sealed class PageModel
{
    public required int PageNumber { get; init; }

    public required SizePt PageSize { get; init; }

    public required Spacing Margins { get; init; }

    public required float ContentTop { get; init; }

    public required float ContentBottom { get; init; }

    public required IReadOnlyList<BlockFragmentPlacement> Placements { get; init; }
}

public sealed class BlockFragmentPlacement
{
    public required int FragmentId { get; init; }

    public required int PageNumber { get; init; }

    public required bool IsOversized { get; init; }

    public required int OrderIndex { get; init; }

    public required BlockFragment Fragment { get; init; }

    // Geometry is derived from the translated fragment to keep one source of truth.
    public float LocalX => Fragment.Rect.X;

    public float LocalY => Fragment.Rect.Y;

    public float Width => Fragment.Rect.Width;

    public float Height => Fragment.Rect.Height;
}

public sealed class PaginationResult
{
    public required IReadOnlyList<PageModel> Pages { get; init; }

    public int TotalPages => Pages.Count;

    public int TotalPlacements => Pages.Sum(static page => page.Placements.Count);
}
