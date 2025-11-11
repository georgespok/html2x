using System.Drawing;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Fragments;

public abstract class Fragment
{
    public RectangleF Rect { get; init; } // absolute page coords (pt)
    public int ZOrder { get; init; } // resolved stacking/z-index
    public VisualStyle Style { get; init; } // minimal style needed to paint this box
}