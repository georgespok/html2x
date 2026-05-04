using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;


internal sealed class LayoutSnapshot
{
    public int PageCount { get; init; }

    public IReadOnlyList<LayoutPageSnapshot> Pages { get; init; } = [];
}
