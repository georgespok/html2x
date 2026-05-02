namespace Html2x.RenderModel;

public sealed record LayoutPage(
    SizePt Size, // full page size in pt (e.g., A4 = 595x842)
    Spacing Margins, // content margins in pt
    IReadOnlyList<Fragment> Children, // fragments positioned in ABSOLUTE page coords.
    // Renderers apply final paint ordering from fragment z-order metadata.
    int PageNumber = 1, // 1-based
    ColorRgba? PageBackground = null // optional (null = white)
)
{
    public SizePt PageSize => Size;
}
