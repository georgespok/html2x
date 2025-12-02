using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf.Rendering;

internal sealed class QuestPdfFragmentRendererFactory : IFragmentRendererFactory
{
    public IFragmentRenderer Create(IContainer container, PdfOptions options, DiagnosticsSession? diagnosticsSession)
    {
        return new QuestPdfFragmentRenderer(container, options, diagnosticsSession);
    }
}


