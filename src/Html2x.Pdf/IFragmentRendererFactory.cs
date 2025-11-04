using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;

namespace Html2x.Pdf;

public interface IFragmentRendererFactory
{
    IFragmentRenderer Create(IContainer container, PdfOptions options, ILoggerFactory? loggerFactory);
}
