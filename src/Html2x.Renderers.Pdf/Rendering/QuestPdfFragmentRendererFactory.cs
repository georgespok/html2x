using Html2x.Renderers.Pdf.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf.Rendering;

internal sealed class QuestPdfFragmentRendererFactory(ILoggerFactory? fallbackFactory = null) : IFragmentRendererFactory
{
    private readonly ILoggerFactory _fallbackFactory = fallbackFactory ?? NullLoggerFactory.Instance;

    public IFragmentRenderer Create(IContainer container, PdfOptions options, ILoggerFactory? loggerFactory)
    {
        var factory = loggerFactory ?? _fallbackFactory;
        var logger = factory.CreateLogger<QuestPdfFragmentRenderer>();
        return new QuestPdfFragmentRenderer(container, options, logger);
    }
}


