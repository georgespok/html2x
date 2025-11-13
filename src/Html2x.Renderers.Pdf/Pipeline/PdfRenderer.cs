using Html2x.Abstractions.Layout.Documents;
using Html2x.Renderers.Pdf.Options;
using Html2x.Renderers.Pdf.Rendering;
using Html2x.Renderers.Pdf.Visitors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Fluent;
using QuestPageSize = QuestPDF.Helpers.PageSize;

namespace Html2x.Renderers.Pdf.Pipeline;

public class PdfRenderer
{
    private readonly IFragmentRendererFactory _rendererFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<PdfRenderer> _logger;
    private readonly ILogger<FragmentRenderDispatcher> _dispatcherLogger;

    public PdfRenderer()
        : this(new QuestPdfFragmentRendererFactory(), null)
    {
    }

    public PdfRenderer(ILoggerFactory? loggerFactory)
        : this(new QuestPdfFragmentRendererFactory(loggerFactory), loggerFactory)
    {
    }

    public PdfRenderer(
        IFragmentRendererFactory rendererFactory,
        ILoggerFactory? loggerFactory = null)
    {
        _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<PdfRenderer>();
        _dispatcherLogger = _loggerFactory.CreateLogger<FragmentRenderDispatcher>();
    }

    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions? options = null)
    {
        options ??= new PdfOptions();
        QuestPdfConfigurator.Configure(options.FontPath, options.LicenseType, options.EnableDebugging);

        if (htmlLayout is null)
        {
            throw new ArgumentNullException(nameof(htmlLayout));
        }

        try
        {
            var bytes = RenderWithQuestPdf(htmlLayout, options);
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            PdfRendererLog.Exception(_logger, ex);
            throw;
        }
    }

    private byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options)
    {
        PdfRendererLog.LayoutStart(_logger, layout.Pages.Count, ComputeOptionsHash(options));

        using var stream = new MemoryStream();

        Document.Create(doc =>
            {
                for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
                {
                    var page = layout.Pages[pageIndex];
                    var pageSize = page.Size;

                    PdfRendererLog.PageStart(_logger, pageIndex, pageSize.Width, pageSize.Height);

                    doc.Page(p =>
                    {
                        p.Size(new QuestPageSize(pageSize.Width, pageSize.Height));
                        p.MarginTop(page.Margins.Top);
                        p.MarginRight(page.Margins.Right);
                        p.MarginBottom(page.Margins.Bottom);
                        p.MarginLeft(page.Margins.Left);

                        p.Content().Column(col =>
                        {
                            var currentY = 0f;

                            foreach (var fragment in page.Children)
                            {
                                PdfRendererLog.FragmentStart(_logger, fragment);

                                var xRel = fragment.Rect.X - page.Margins.Left;
                                var yRel = fragment.Rect.Y - page.Margins.Top;

                                var deltaY = Math.Max(0, yRel - currentY);
                                if (deltaY > 0)
                                {
                                    col.Item().Height(deltaY);
                                    currentY += deltaY;
                                }

                                col.Item().Row(row =>
                                {
                                    if (xRel > 0)
                                    {
                                        row.ConstantItem(xRel).Element(_ => { });
                                    }

                                    row.ConstantItem(fragment.Rect.Width).Element(box =>
                                    {
                                        var target = fragment.Rect.Height > 0
                                            ? box.MinHeight(fragment.Rect.Height)
                                            : box;

                                        var renderer = _rendererFactory.Create(target, options, _loggerFactory);
                                        var dispatcher = new FragmentRenderDispatcher(renderer, _dispatcherLogger);
                                        fragment.VisitWith(dispatcher);
                                    });
                                });

                                currentY = Math.Max(currentY, yRel + fragment.Rect.Height);
                            }
                        });
                    });
                }
            })
            .GeneratePdf(stream);

        return stream.ToArray();
    }

    private static int ComputeOptionsHash(PdfOptions options)
    {
        return HashCode.Combine(
            options.FontPath is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(options.FontPath),
            options.LicenseType,
            options.PageSize.GetHashCode());
    }
}





