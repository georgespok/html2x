using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Documents;

public sealed record LayoutPage(
    SizeF Size, // full page size in pt (e.g., A4 = 595x842)
    Spacing Margins, // content margins in pt
    IReadOnlyList<Fragment> Children, // fragments positioned in ABSOLUTE page coords,
    // are already paint-ordered per stacking/z-index.
    int PageNumber = 1, // 1-based
    ColorRgba? PageBackground = null // optional (null = white)
)
{
    public RectangleF ContentRect =>
        new(
            Margins.Left,
            Margins.Top,
            Size.Width - Margins.Left - Margins.Right,
            Size.Height - Margins.Top - Margins.Bottom
        );
}