using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Options;
using Html2x.Files;
using Html2x.Fonts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.Renderers.Pdf.Pipeline;

namespace Html2x;

public class HtmlConverter
{
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

        IFontSource fontSource = new FontPathSource(fontPath, fileDirectory);
        if (session is not null)
        {
            fontSource = new DiagnosticsFontSource(fontSource, session);
        }

        var measurer = new SkiaTextMeasurer(fontSource);
        var imageProvider = new FileSystemImageProvider();

        session?.Events.Add(DiagnosticsEventFactory.StageStarted(
            "LayoutBuild",
            new HtmlPayload { Html = html.Trim() }));

        var layoutBuilder = new LayoutBuilder(measurer, fontSource, imageProvider);

        HtmlLayout layout;
        try
        {
            layout = await layoutBuilder.BuildAsync(html, options.Layout, session);
        }
        catch (Exception exception)
        {
            session?.Events.Add(DiagnosticsEventFactory.StageFailed("LayoutBuild", exception.Message));
            session?.Events.Add(DiagnosticsEventFactory.StageSkipped("PdfRender", "Skipped because LayoutBuild failed."));
            AttachDiagnostics(exception, session);
            throw;
        }

        session?.Events.Add(DiagnosticsEventFactory.StageSucceeded(
            "LayoutBuild",
            new LayoutSnapshotPayload
            {
                Snapshot = LayoutSnapshotMapper.From(layout)
            }));

        var renderer = new PdfRenderer(fileDirectory);

        session?.Events.Add(DiagnosticsEventFactory.StageStarted(
            "PdfRender",
            null));

        byte[] pdfBytes;
        try
        {
            pdfBytes = await renderer.RenderAsync(layout, options.Pdf, session, fontSource);
        }
        catch (Exception exception)
        {
            session?.Events.Add(DiagnosticsEventFactory.StageFailed("PdfRender", exception.Message));
            AttachDiagnostics(exception, session);
            throw;
        }

        session?.Events.Add(DiagnosticsEventFactory.StageSucceeded(
            "PdfRender", new RenderSummaryPayload()
            {
                PdfSize = pdfBytes.Length,
                PageCount = layout.Pages.Count
            }));

        return new Html2PdfResult(pdfBytes)
        {
            Diagnostics = session
        };
    }

    private static void AddDiagnosticsEvent(
        DiagnosticsSession? session,
        DiagnosticsEventType type,
        string name,
        IDiagnosticsPayload? payload) =>
        session?.Events.Add(new DiagnosticsEvent
        {
            Type = type,
            Name = name,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = payload
        });

    private static InvalidOperationException CreateFontPathException(string message, DiagnosticsSession? session)
    {
        AddDiagnosticsEvent(session, DiagnosticsEventType.Error, "FontPath", null);
        session?.Events.Add(DiagnosticsEventFactory.StageFailed("LayoutBuild", message));
        session?.Events.Add(DiagnosticsEventFactory.StageSkipped("PdfRender", "Skipped because LayoutBuild failed."));

        var exception = new InvalidOperationException(message);
        if (session is not null)
        {
            exception.Data["Diagnostics"] = session;
        }

        return exception;
    }

    private static void AttachDiagnostics(Exception exception, DiagnosticsSession? session)
    {
        if (session is null)
        {
            return;
        }

        exception.Data["Diagnostics"] = session;
    }
}



