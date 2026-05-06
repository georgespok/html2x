using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Diagnostics;

internal sealed class LayoutPageSnapshot
{
    public int PageNumber { get; init; }

    public SizePt PageSize { get; init; }

    public Spacing Margin { get; init; }

    public IReadOnlyList<FragmentSnapshot> Fragments { get; init; } = [];
}