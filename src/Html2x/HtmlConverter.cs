using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Files;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x;

public class HtmlConverter
{
    private readonly ILayoutBuilderFactory _layoutBuilderFactory = new LayoutBuilderFactory();

    public async Task<Html2PdfResult> ToPdfAsync(string html, HtmlConverterOptions options)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        options ??= new HtmlConverterOptions();

        DiagnosticsSession? session = null;
        if (options.Diagnostics.EnableDiagnostics)
        {
            session = new DiagnosticsSession
            {
                StartTime = DateTimeOffset.UtcNow,
                Options = options
            };
        }

        AddDiagnosticsEvent(
            session,
            DiagnosticsEventType.StartStage,
            "LayoutBuild",
            new HtmlPayload { Html = html.Trim() });

        var layoutBuilder = _layoutBuilderFactory.Create()
                            ?? throw new InvalidOperationException("Layout factory returned null.");

        var layout = await layoutBuilder.BuildAsync(html, options.Layout, session);

        AddDiagnosticsEvent(
            session,
            DiagnosticsEventType.EndStage,
            "LayoutBuild",
            new LayoutSnapshotPayload
            {
                Snapshot = LayoutSnapshotMapper.From(layout)
            });

        var renderer = new PdfRenderer(new FileDirectory());

        AddDiagnosticsEvent(
            session,
            DiagnosticsEventType.StartStage,
            "PdfRender",
            null);

        var pdfBytes = await renderer.RenderAsync(layout, options.Pdf, session);

        AddDiagnosticsEvent(
            session,
            DiagnosticsEventType.EndStage,
            "PdfRender", new RenderSummaryPayload()
            {
                PdfSize = pdfBytes.Length,
                PageCount = layout.Pages.Count
            });

        return new Html2PdfResult(pdfBytes)
        {
            Diagnostics = session
        };
    }

    private static void AddDiagnosticsEvent(
        DiagnosticsSession? session,
        DiagnosticsEventType type,
        string name,
        IDiagnosticsPayload? payload)
    {
        if (session is null)
        {
            return;
        }

        session.Events.Add(new DiagnosticsEvent
        {
            Type = type,
            Name = name,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = payload
        });
    }
}



