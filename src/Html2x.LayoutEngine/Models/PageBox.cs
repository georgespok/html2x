using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine.Models;

public sealed class PageBox
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);

    // (MVP) fixed page size A4
    public SizePt Size { get; set; } = PaperSizes.A4;
}
