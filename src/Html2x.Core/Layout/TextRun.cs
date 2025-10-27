using System.Drawing;

namespace Html2x.Core.Layout;

public sealed record TextRun(
    FontKey Font,                   // family/weight/style resolved to a font key
    float FontSizePt,
    PointF Origin,                  // baseline origin (absolute)
    IReadOnlyList<GlyphPos> Glyphs, // shaped glyph id + pos (preferred)
    TextDecorations Decorations = TextDecorations.None
);

[Flags]
public enum TextDecorations { None = 0, Underline = 1, Overline = 2, LineThrough = 4 }