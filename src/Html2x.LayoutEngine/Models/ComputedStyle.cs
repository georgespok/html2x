using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Models;

public sealed record ComputedStyle
{
    public string FontFamily { get; init; } = HtmlCssConstants.Defaults.FontFamily;
    public float FontSizePt { get; init; } = 12;
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public TextDecorations Decorations { get; init; }
    public string TextAlign { get; init; } = HtmlCssConstants.Defaults.TextAlign;
    public float LineHeightMultiplier { get; init; }
    public ColorRgba Color { get; init; } = ColorRgba.Black;
    public ColorRgba? BackgroundColor { get; init; }
    public float MarginTopPt { get; init; }
    public float MarginRightPt { get; init; }
    public float MarginBottomPt { get; init; }
    public float MarginLeftPt { get; init; }
    public float PaddingTopPt { get; init; }
    public float PaddingRightPt { get; init; }
    public float PaddingBottomPt { get; init; }
    public float PaddingLeftPt { get; init; }
    public float? MaxWidthPt { get; init; }
    public BorderEdges Borders { get; init; } = BorderEdges.None;
}
