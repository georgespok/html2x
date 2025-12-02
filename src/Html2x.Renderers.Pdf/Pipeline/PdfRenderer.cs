using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Options;
using Html2x.Renderers.Pdf.Rendering;
using Html2x.Renderers.Pdf.Visitors;
using Html2x.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPageSize = QuestPDF.Helpers.PageSize;

namespace Html2x.Renderers.Pdf.Pipeline;

public class PdfRenderer
{
    private readonly IFragmentRendererFactory _rendererFactory;
    private DiagnosticsSession? _diagnosticsSession;

    public PdfRenderer()
        : this(new QuestPdfFragmentRendererFactory())
    {
    }

    public PdfRenderer(
        IFragmentRendererFactory rendererFactory)
    {
        _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
     
    }

    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions? options = null, DiagnosticsSession? diagnosticsSession = null)
    {
        options ??= new PdfOptions();
        QuestPdfConfigurator.Configure(options.FontPath, options.LicenseType, options.EnableDebugging);
        _diagnosticsSession = diagnosticsSession;

        if (htmlLayout is null)
        {
            throw new ArgumentNullException(nameof(htmlLayout));
        }

        
        try
        {
            var bytes = RenderWithQuestPdf(htmlLayout, options, diagnosticsSession);
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            
            throw;
        }
    }

    private byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options, DiagnosticsSession? diagnosticsSession)
    {
        PublishLayoutStart(layout, options);

        using var stream = new MemoryStream();

        Document.Create(doc => ConfigureDocument(doc, layout, options))
            .GeneratePdf(stream);

        return stream.ToArray();
    }

    private void ConfigureDocument(IDocumentContainer document, HtmlLayout layout, PdfOptions options)
    {
        for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
        {
            var page = layout.Pages[pageIndex];
            PublishPageStart(pageIndex, page);

            document.Page(descriptor => ConfigurePage(descriptor, page, options));
        }
    }

    private void ConfigurePage(PageDescriptor pageDescriptor, LayoutPage page, PdfOptions options)
    {
        var pageSize = page.Size;

        pageDescriptor.Size(new QuestPageSize(pageSize.Width, pageSize.Height));
        pageDescriptor.MarginTop(page.Margins.Top);
        pageDescriptor.MarginRight(page.Margins.Right);
        pageDescriptor.MarginBottom(page.Margins.Bottom);
        pageDescriptor.MarginLeft(page.Margins.Left);

        pageDescriptor.Content().Column(column => WriteFragments(column, page, options));
    }

    private void WriteFragments(ColumnDescriptor column, LayoutPage page, PdfOptions options)
    {
        var currentY = 0f;

        foreach (var fragment in page.Children)
        {
            var xRel = fragment.Rect.X - page.Margins.Left;
            var yRel = fragment.Rect.Y - page.Margins.Top;

            InsertVerticalSpacer(column, ref currentY, yRel);

            column.Item().Row(row =>
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

                    var renderer = _rendererFactory.Create(target, options, _diagnosticsSession);
                    var dispatcher = new FragmentRenderDispatcher(renderer);
                    fragment.VisitWith(dispatcher);
                });
            });

            currentY = Math.Max(currentY, yRel + fragment.Rect.Height);
        }
    }

    private static void InsertVerticalSpacer(ColumnDescriptor column, ref float currentY, float targetY)
    {
        var deltaY = Math.Max(0, targetY - currentY);
        if (deltaY <= 0)
        {
            return;
        }

        column.Item().Height(deltaY);
        currentY += deltaY;
    }

    private static int ComputeOptionsHash(PdfOptions options)
    {
        return HashCode.Combine(
            options.FontPath is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(options.FontPath),
            options.LicenseType,
            options.PageSize.GetHashCode());
    }

    private void PublishLayoutStart(HtmlLayout layout, PdfOptions options)
    {
        
    }

    private void PublishPageStart(int pageIndex, LayoutPage page)
    {
        var pageSize = page.Size;

        
    }

}
