using System.Drawing;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Fragments;

/// <summary>
/// Represents a measured run of text with absolute baseline origin and resolved font data.
/// </summary>
public sealed record TextRun(
    string Text, // exact substring for this run (post line-wrapping)
    FontKey Font,
    float FontSizePt,
    PointF Origin, // baseline origin (absolute)
    float AdvanceWidth, // measured width used during line-breaking
    float Ascent, // font metrics for baseline alignment
    float Descent,
    TextDecorations Decorations = TextDecorations.None,
    ColorRgba? Color = null,
    ResolvedFont? ResolvedFont = null
);

/// <summary>
/// Defines text decoration flags used when painting a text run.
/// </summary>
[Flags]
public enum TextDecorations
{
    None = 0,
    Underline = 1,
    Overline = 2,
    LineThrough = 4
}
