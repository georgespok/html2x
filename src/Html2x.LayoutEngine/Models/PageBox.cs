using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed class PageBox
{
    public Spacing Margin { get; set; } = new(24, 24, 24, 24);

    // (MVP) fixed page size A4
    public float PageWidthPt { get; set; } = 595;
    public float PageHeightPt { get; set; } = 842;
}
