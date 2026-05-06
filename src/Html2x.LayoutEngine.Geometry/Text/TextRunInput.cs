using Html2x.RenderModel.Text;

namespace Html2x.LayoutEngine.Geometry.Text;

/// <summary>
///     Carries source text, spacing, and font data for inline measurement.
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