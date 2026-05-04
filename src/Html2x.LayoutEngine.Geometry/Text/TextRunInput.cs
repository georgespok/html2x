using Html2x.RenderModel;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Text;


/// <summary>
/// Carries source text, spacing, and font data for inline measurement.
/// </summary>
internal sealed record TextRunInput(
    int RunId,
    InlineBox Source,
    string Text,
    FontKey Font,
    float FontSizePt,
    ComputedStyle Style,
    float PaddingLeft,
    float PaddingRight,
    float MarginLeft,
    float MarginRight,
    TextRunKind Kind = TextRunKind.Normal,
    InlineObjectLayout? InlineObject = null);
