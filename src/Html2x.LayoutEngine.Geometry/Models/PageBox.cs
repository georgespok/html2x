using Html2x.RenderModel;

namespace Html2x.LayoutEngine.Models;

internal sealed class PageBox
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);

    // (MVP) fixed page size A4
    public SizePt Size { get; set; } = PaperSizes.A4;
}