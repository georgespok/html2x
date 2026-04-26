using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Paint;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Compatibility facade over the paint command pipeline for callers that still draw pages directly.
/// </summary>
internal sealed class SkiaFragmentDrawer
{
    private readonly PaintCommandBuilder _commandBuilder = new();
    private readonly SkiaPaintCommandDrawer _commandDrawer;

    public SkiaFragmentDrawer(PdfOptions options, DiagnosticsSession? diagnosticsSession, SkiaFontCache fontCache)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fontCache);

        _commandDrawer = new SkiaPaintCommandDrawer(options, diagnosticsSession, fontCache);
    }

    public void DrawPage(SKCanvas canvas, LayoutPage page)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(page);

        _commandDrawer.Draw(canvas, _commandBuilder.Build(page));
    }
}
