using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed class GeometrySnapshot
{
    public LayoutSnapshot Fragments { get; init; } = new();

    public IReadOnlyList<BoxGeometrySnapshot> Boxes { get; init; } = [];

    public IReadOnlyList<PaginationPageSnapshot> Pagination { get; init; } = [];
}
