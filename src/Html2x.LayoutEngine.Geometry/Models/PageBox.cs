using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class PageBox
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);

    // Default page size; conversion requests may override it.
    public SizePt Size { get; set; } = PaperSizes.A4;
}
