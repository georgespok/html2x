using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Models;

internal sealed class PageBox
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);

    // (MVP) fixed page size A4
    public SizePt Size { get; set; } = PaperSizes.A4;
}