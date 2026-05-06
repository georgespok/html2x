namespace Html2x.Renderers.Pdf.Paint;

/// <summary>
///     Identifies the supported internal paint command categories for current PDF rendering.
/// </summary>
internal enum PaintCommandKind
{
    PageBackground,
    Background,
    Border,
    Text,
    Image,
    Rule
}