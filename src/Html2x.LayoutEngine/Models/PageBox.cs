namespace Html2x.LayoutEngine.Models;

public sealed class PageBox
{
    public float MarginTopPt { get; set; }
    public float MarginRightPt { get; set; }
    public float MarginBottomPt { get; set; }

    public float MarginLeftPt { get; set; }

    // (MVP) fixed page size A4
    public float PageWidthPt { get; set; } = 595;
    public float PageHeightPt { get; set; } = 842;
}