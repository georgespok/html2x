using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

internal sealed class QuestPdfFragmentRendererFactory : IFragmentRendererFactory
{
    public IFragmentRenderer Create(IContainer container, PdfOptions options)
        => new QuestPdfFragmentRenderer(container, options);
}
