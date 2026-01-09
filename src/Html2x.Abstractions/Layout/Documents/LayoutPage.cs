using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Layout.Documents;

public sealed record LayoutPage(
    SizePt Size, // full page size in pt (e.g., A4 = 595x842)
    Spacing Margins, // content margins in pt
    IReadOnlyList<Fragment> Children, // fragments positioned in ABSOLUTE page coords,
    // are already paint-ordered per stacking/z-index.
    int PageNumber = 1, // 1-based
    ColorRgba? PageBackground = null // optional (null = white)
)
{
    public SizePt PageSize => Size;
}
