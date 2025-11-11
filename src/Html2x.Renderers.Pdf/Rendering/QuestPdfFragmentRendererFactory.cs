using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;

using Html2x.Pdf.Options;
using Html2x.Renderers.Pdf.Rendering;

namespace Html2x.Pdf.Rendering;

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


