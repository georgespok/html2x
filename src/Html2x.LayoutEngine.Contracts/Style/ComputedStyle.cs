using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Contracts.Style;

/// <summary>
///     Stores the computed style facts consumed by box construction, layout, diagnostics, and fragment tree building.
/// </summary>
internal sealed record ComputedStyle
{
    public string FontFamily { get; init; } = HtmlCssVocabulary.Defaults.FontFamily;
    public float FontSizePt { get; init; } = 12;
    public bool IsBold { get; init; }
    public bool IsItalic { get; init; }
    public TextDecorations Decorations { get; init; }
    public string TextAlign { get; init; } = HtmlCssVocabulary.Defaults.TextAlign;
    public float LineHeightMultiplier { get; init; }
    public ColorRgba Color { get; init; } = ColorRgba.Black;
    public ColorRgba? BackgroundColor { get; init; }
    public string? Display { get; init; }
    public string FloatDirection { get; init; } = HtmlCssVocabulary.Defaults.FloatDirection;
    public string Position { get; init; } = HtmlCssVocabulary.Defaults.Position;
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