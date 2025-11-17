using System.Drawing;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Layout.Fragments;

public sealed record TextRun(
    string Text, // exact substring for this run (post line-wrapping)
    FontKey Font,
    float FontSizePt,
    PointF Origin, // baseline origin (absolute)
    float AdvanceWidth, // measured width used during line-breaking
    float Ascent, // font metrics for baseline alignment
    float Descent,
    TextDecorations Decorations = TextDecorations.None,
    string? ColorHex = null
);

[Flags]
public enum TextDecorations
{
    None = 0,
    Underline = 1,
    Overline = 2,
    LineThrough = 4
}
