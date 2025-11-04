using Html2x.Core.Layout;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Html2x.Pdf;

public class PdfRenderer
{
    private readonly IFragmentRendererFactory _rendererFactory;

    public PdfRenderer()
        : this(new QuestPdfFragmentRendererFactory())
    {
    }

    public PdfRenderer(IFragmentRendererFactory rendererFactory)
    {
        _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
    }

    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions? options = null)
    {
        QuestPdfConfigurator.Configure(options?.FontPath, options?.LicenseType ?? PdfLicenseType.Community);

        if (htmlLayout is null)
        {
            throw new ArgumentNullException(nameof(htmlLayout));
        }

        options ??= new PdfOptions();

        var bytes = RenderWithQuestPdf(htmlLayout, options);
        return Task.FromResult(bytes);
    }

    private byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options)
    {
        using var stream = new MemoryStream();

        Document.Create(doc =>
            {
                foreach (var page in layout.Pages)
                {
                    var pageSize = page.Size;

                    doc.Page(p =>
                    {
                        p.Size(new PageSize(pageSize.Width, pageSize.Height));
                        p.MarginTop(page.Margins.Top);
                        p.MarginRight(page.Margins.Right);
                        p.MarginBottom(page.Margins.Bottom);
                        p.MarginLeft(page.Margins.Left);

                        p.Content().Column(col =>
                        {
                            var currentY = 0f; // relative to content area (after page margins)

                            foreach (var fragment in page.Children)
                            {
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
                                    // left offset spacer
                                    if (xRel > 0)
                                    {
                                        row.ConstantItem(xRel).Element(_ => { });
                                    }

                                    row.ConstantItem(fragment.Rect.Width).Element(box =>
                                    {
                                        var target = fragment.Rect.Height > 0
                                            ? box.MinHeight(fragment.Rect.Height)
                                            : box;

                                        var renderer = _rendererFactory.Create(target, options);
                                        var dispatcher = new FragmentRenderDispatcher(renderer);
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
}
