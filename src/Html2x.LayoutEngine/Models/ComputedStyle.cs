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
    public string? Display { get; init; }
    public Spacing Margin { get; init; } = new(0, 0, 0, 0);
    public Spacing Padding { get; init; } = new(0, 0, 0, 0);
    public float? WidthPt { get; init; }
    public float? MinWidthPt { get; init; }
    public float? MaxWidthPt { get; init; }
    public float? HeightPt { get; init; }
    public float? MinHeightPt { get; init; }
    public float? MaxHeightPt { get; init; }
    public BorderEdges Borders { get; init; } = BorderEdges.None;
}
