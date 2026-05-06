using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;

namespace Html2x.LayoutEngine.Pagination;

internal sealed class BlockFragmentPlacement
{
    public required int FragmentId { get; init; }

    public required int PageNumber { get; init; }

    public required PaginationDecisionKind DecisionKind { get; init; }

    public required bool IsOversized { get; init; }

    public required int OrderIndex { get; init; }

    public required BlockFragment Fragment { get; init; }

    public RectPt PlacedRect => Fragment.Rect;

    public float PageX => Fragment.Rect.X;

    public float PageY => Fragment.Rect.Y;

    public float Width => Fragment.Rect.Width;

    public float Height => Fragment.Rect.Height;
}