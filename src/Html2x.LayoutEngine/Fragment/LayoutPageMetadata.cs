using Html2x.Core.Layout;

namespace Html2x.LayoutEngine.Fragment;

public sealed class LayoutPageMetadata
{
    public float PageWidthPt { get; set; } = 595;
    public float PageHeightPt { get; set; } = 842;
    public Spacing Margins { get; set; } = new(24, 24, 24, 24);
}