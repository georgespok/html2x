using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;

using Html2x.Pdf.Options;
namespace Html2x.Pdf.Rendering;

public interface IFragmentRendererFactory
{
    IFragmentRenderer Create(IContainer container, PdfOptions options, ILoggerFactory? loggerFactory);
}


