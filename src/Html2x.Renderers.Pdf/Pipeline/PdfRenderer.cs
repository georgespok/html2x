using System;
using System.Collections.Generic;
using System.IO;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Renderers.Pdf.Options;
using Html2x.Renderers.Pdf.Rendering;
using Html2x.Renderers.Pdf.Visitors;
using QuestPDF.Fluent;
using QuestPageSize = QuestPDF.Helpers.PageSize;

namespace Html2x.Renderers.Pdf.Pipeline;

public class PdfRenderer
{
    private readonly IFragmentRendererFactory _rendererFactory;
    private readonly IDiagnosticSession? _diagnosticSession;

    public PdfRenderer()
        : this(new QuestPdfFragmentRendererFactory())
    {
    }

    public PdfRenderer(IDiagnosticSession? diagnosticSession)
        : this(new QuestPdfFragmentRendererFactory(), diagnosticSession)
    {
    }

    public PdfRenderer(
        IFragmentRendererFactory rendererFactory,
        IDiagnosticSession? diagnosticSession = null)
    {
        _rendererFactory = rendererFactory ?? throw new ArgumentNullException(nameof(rendererFactory));
        _diagnosticSession = diagnosticSession;
    }

    public Task<byte[]> RenderAsync(HtmlLayout htmlLayout, PdfOptions? options = null)
    {
        options ??= new PdfOptions();
        QuestPdfConfigurator.Configure(options.FontPath, options.LicenseType, options.EnableDebugging);

        if (htmlLayout is null)
        {
            throw new ArgumentNullException(nameof(htmlLayout));
        }

        using var scope = DiagnosticsStageScope.Begin(_diagnosticSession, "stage/pdf-render");

        try
        {
            var bytes = RenderWithQuestPdf(htmlLayout, options);
            return Task.FromResult(bytes);
        }
        catch (Exception ex)
        {
            PublishRenderEvent("render/pdf/error", payload =>
            {
                payload["message"] = ex.Message;
            });

            throw;
        }
    }

    private byte[] RenderWithQuestPdf(HtmlLayout layout, PdfOptions options)
    {
        PublishRenderEvent("render/pdf/layout-start", payload =>
        {
            payload["pages"] = layout.Pages.Count;
            payload["optionsHash"] = ComputeOptionsHash(options);
        });

        using var stream = new MemoryStream();

        Document.Create(doc =>
            {
                for (var pageIndex = 0; pageIndex < layout.Pages.Count; pageIndex++)
                {
                    var page = layout.Pages[pageIndex];
                    var pageSize = page.Size;

                    PublishRenderEvent("render/pdf/page-start", payload =>
                    {
                        payload["pageIndex"] = pageIndex;
                        payload["width"] = pageSize.Width;
                        payload["height"] = pageSize.Height;
                    });

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

                                        var renderer = _rendererFactory.Create(target, options, _diagnosticSession);
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

    private static int ComputeOptionsHash(PdfOptions options)
    {
        return HashCode.Combine(
            options.FontPath is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(options.FontPath),
            options.LicenseType,
            options.PageSize.GetHashCode());
    }
    private void PublishRenderEvent(string kind, Action<Dictionary<string, object?>> configure)
    {
        if (_diagnosticSession is not { IsEnabled: true })
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        configure(payload);

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            _diagnosticSession.Descriptor.SessionId,
            "render/pdf",
            kind,
            DateTimeOffset.UtcNow,
            payload);

        _diagnosticSession.Publish(diagnosticEvent);
    }
}
