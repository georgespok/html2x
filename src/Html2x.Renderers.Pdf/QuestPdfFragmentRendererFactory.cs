using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;

namespace Html2x.Renderers.Pdf;

internal sealed class QuestPdfFragmentRendererFactory : IFragmentRendererFactory
{
    private readonly ILoggerFactory _fallbackFactory;

    public QuestPdfFragmentRendererFactory(ILoggerFactory? fallbackFactory = null)
    {
        _fallbackFactory = fallbackFactory ?? NullLoggerFactory.Instance;
    }

    public IFragmentRenderer Create(IContainer container, PdfOptions options, ILoggerFactory? loggerFactory)
    {
        var factory = loggerFactory ?? _fallbackFactory;
        var logger = factory.CreateLogger<QuestPdfFragmentRenderer>();
        return new QuestPdfFragmentRenderer(container, options, logger);
    }
}
