using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed class ComputedStyle
{
    public string FontFamily { get; set; } = HtmlCssConstants.Defaults.FontFamily;
    public float FontSizePt { get; set; } = 12;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string TextAlign { get; set; } = HtmlCssConstants.Defaults.TextAlign;
    public float? LineHeightMultiplier { get; set; }
    public ColorRgba Color { get; set; } = ColorRgba.Black;
    public ColorRgba? BackgroundColor { get; set; }
    public float MarginTopPt { get; set; }
    public float MarginRightPt { get; set; }
    public float MarginBottomPt { get; set; }
    public float MarginLeftPt { get; set; }
    public float PaddingTopPt { get; set; }
    public float PaddingRightPt { get; set; }
    public float PaddingBottomPt { get; set; }
    public float PaddingLeftPt { get; set; }
    public float? MaxWidthPt { get; set; }
    public BorderEdges Borders { get; set; } = BorderEdges.None;
}
