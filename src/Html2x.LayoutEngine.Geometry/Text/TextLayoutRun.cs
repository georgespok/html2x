using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Represents one measured run after line wrapping.
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
    InlineObjectLayout? InlineObject = null);
