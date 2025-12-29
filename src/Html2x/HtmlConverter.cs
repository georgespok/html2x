using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Html2x.Files;
using Html2x.Fonts;
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

        var fileDirectory = new FileDirectory();
        var fontPath = options.Pdf.FontPath;
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw CreateFontPathException(
                "PdfOptions.FontPath must be provided before layout can begin.",
                session);
        }

        if (!fileDirectory.FileExists(fontPath) && !fileDirectory.DirectoryExists(fontPath))
        {
            throw CreateFontPathException(
                $"PdfOptions.FontPath '{fontPath}' does not exist.",
                session);
        }

        var fontSource = new FontPathSource(fontPath, fileDirectory);
        var measurer = new SkiaTextMeasurer(fontSource);

        AddDiagnosticsEvent(
            session,
            DiagnosticsEventType.StartStage,
            "LayoutBuild",
            new HtmlPayload { Html = html.Trim() });

        var layoutBuilder = _layoutBuilderFactory.Create(new LayoutServices(measurer, fontSource))
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

        var renderer = new PdfRenderer(fileDirectory);

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

    private static InvalidOperationException CreateFontPathException(string message, DiagnosticsSession? session)
    {
        AddDiagnosticsEvent(session, DiagnosticsEventType.Error, "FontPath", null);

        var exception = new InvalidOperationException(message);
        if (session is not null)
        {
            exception.Data["Diagnostics"] = session;
        }

        return exception;
    }
}



