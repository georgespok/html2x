using Html2x.Core.Layout;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Linq;

namespace Html2x.Pdf;

public class PdfRenderer
{
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

    private static byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options)
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
                                        row.ConstantItem(xRel).Element(e => { });
                                    }

                                    // fragment box with fixed size
                                    row.ConstantItem(fragment.Rect.Width).Height(fragment.Rect.Height).Element(box =>
                                    {
                                        var renderer = new FragmentRenderer(box, options);
                                        fragment.VisitWith(renderer);
                                    });
                                });

                                currentY = yRel + fragment.Rect.Height;
                            }
                        });
                    });
                }
            })
            .GeneratePdf(stream);

        return stream.ToArray();
    }
}