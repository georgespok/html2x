using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics.Contracts;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Compatibility facade over the paint command pipeline for callers that still draw pages directly.
/// </summary>
internal sealed class SkiaFragmentDrawer
{
    private readonly PaintOrderResolver _paintOrder = new();
    private readonly SkiaPaintCommandDrawer _commandDrawer;

    public SkiaFragmentDrawer(
        PdfOptions options,
        SkiaFontCache fontCache,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fontCache);

        _commandDrawer = new SkiaPaintCommandDrawer(options, fontCache, diagnosticsSink);
    }

    public void DrawPage(SKCanvas canvas, LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(page);

        _commandDrawer.Draw(canvas, _paintOrder.Resolve(page));
    }
}
