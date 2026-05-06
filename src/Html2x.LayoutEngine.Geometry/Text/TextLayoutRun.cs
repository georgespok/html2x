using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Represents one measured run after line wrapping.
/// </summary>
internal sealed record TextLayoutRun(
    InlineBox Source,
    string Text,
    FontKey Font,
    float FontSizePt,
    float Width,
    float LeftSpacing,
    float RightSpacing,
    float Ascent,
    float Descent,
    TextDecorations Decorations,
    ColorRgba? Color,
    ResolvedFont? ResolvedFont = null,
    InlineBoxLayout? InlineBox = null);
