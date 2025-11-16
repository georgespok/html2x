using Html2x.Abstractions.Diagnostics;
using Html2x.Renderers.Pdf.Options;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf.Rendering;

public interface IFragmentRendererFactory
{
    IFragmentRenderer Create(IContainer container, PdfOptions options, IDiagnosticSession? diagnosticSession);
}


