using Html2x.Abstractions.Diagnostics;
using Html2x.Renderers.Pdf.Options;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf.Rendering;

internal sealed class QuestPdfFragmentRendererFactory : IFragmentRendererFactory
{
    public IFragmentRenderer Create(IContainer container, PdfOptions options, IDiagnosticSession? diagnosticSession)
    {
        return new QuestPdfFragmentRenderer(container, options, diagnosticSession);
    }
}


