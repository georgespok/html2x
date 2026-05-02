using Html2x.RenderModel;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Style;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Text;

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

        DiagnosticsCollector? collector = null;
        IDiagnosticsSink? diagnosticsSink = null;
        if (options.Diagnostics.EnableDiagnostics)
        {
            var diagnosticsStartTime = DateTimeOffset.UtcNow;
            collector = new DiagnosticsCollector(diagnosticsStartTime);
            diagnosticsSink = collector;
        }

        var fontPath = options.Fonts.FontPath;
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw CreateFontPathException(
                "HtmlConverterOptions.Fonts.FontPath must be provided before layout can begin.",
                collector);
        }

        IFontSource fontSource;
        try
        {
            fontSource = new FontPathSource(fontPath);
        }
        catch (InvalidOperationException exception) when (IsFontPathException(exception))
        {
            throw CreateFontPathException(
                $"HtmlConverterOptions.Fonts.FontPath '{fontPath}' does not exist.",
                collector);
        }

        if (diagnosticsSink is not null)
        {
            fontSource = new DiagnosticsFontSource(fontSource, diagnosticsSink);
        }

        var measurer = new SkiaTextMeasurer(fontSource);
        var imageMetadataResolver = new FileImageProvider();

        EmitStageStarted(
            diagnosticsSink,
            "LayoutBuild",
            DiagnosticFields.Create(DiagnosticFields.Field("html", html.Trim())));

        var layoutBuilder = new LayoutBuilder(measurer, imageMetadataResolver);

        HtmlLayout layout;
        try
        {
            layout = await layoutBuilder.BuildAsync(html, ToLayoutBuildSettings(options), diagnosticsSink);
        }
        catch (Exception exception)
        {
            EmitStageFailed(diagnosticsSink, "LayoutBuild", exception.Message);
            EmitStageSkipped(diagnosticsSink, "PdfRender", "Skipped because LayoutBuild failed.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        EmitStageSucceeded(
            diagnosticsSink,
            "LayoutBuild",
            DiagnosticFields.Create(
                DiagnosticFields.Field("snapshot", LayoutSnapshotMapper.ToDiagnosticObject(layout))));

        var renderer = new PdfRenderer();

        EmitStageStarted(diagnosticsSink, "PdfRender");

        byte[] pdfBytes;
        try
        {
            pdfBytes = await renderer.RenderAsync(layout, ToPdfRenderSettings(options), diagnosticsSink: diagnosticsSink);
        }
        catch (Exception exception)
        {
            EmitStageFailed(diagnosticsSink, "PdfRender", exception.Message);
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        EmitStageSucceeded(
            diagnosticsSink,
            "PdfRender",
            DiagnosticFields.Create(
                DiagnosticFields.Field("pdfSize", pdfBytes.Length),
                DiagnosticFields.Field("pageCount", layout.Pages.Count)));

        var report = CompleteDiagnostics(collector);

        return new Html2PdfResult(pdfBytes)
        {
            DiagnosticsReport = report
        };
    }

    private static LayoutBuildSettings ToLayoutBuildSettings(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new LayoutBuildSettings
        {
            PageSize = options.Page.Size,
            HtmlDirectory = options.Resources.BaseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes,
            Style = new StyleBuildSettings
            {
                UseDefaultUserAgentStyleSheet = options.Css.UseDefaultUserAgentStyleSheet,
                UserAgentStyleSheet = options.Css.UserAgentStyleSheet
            }
        };
    }

    private static PdfRenderSettings ToPdfRenderSettings(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new PdfRenderSettings
        {
            HtmlDirectory = options.Resources.BaseDirectory
        };
    }

    private static InvalidOperationException CreateFontPathException(
        string message,
        DiagnosticsCollector? collector)
    {
        IDiagnosticsSink? diagnosticsSink = collector;
        EmitDiagnosticsRecord(diagnosticsSink, "Configuration", "font-path/error", DiagnosticSeverity.Error, message);
        EmitStageFailed(diagnosticsSink, "LayoutBuild", message);
        EmitStageSkipped(diagnosticsSink, "PdfRender", "Skipped because LayoutBuild failed.");

        var exception = new InvalidOperationException(message);
        AttachDiagnosticsReport(exception, collector);
        return exception;
    }

    private static void AttachDiagnosticsReport(
        Exception exception,
        DiagnosticsCollector? collector)
    {
        var report = CompleteDiagnostics(collector);
        if (report is not null)
        {
            exception.Data["DiagnosticsReport"] = report;
        }
    }

    private static DiagnosticsReport? CompleteDiagnostics(DiagnosticsCollector? collector)
    {
        var endTime = DateTimeOffset.UtcNow;
        return collector?.ToReport(endTime);
    }

    private static bool IsFontPathException(Exception exception) =>
        exception.Data["DiagnosticsName"] as string == "FontPath";

    private static void EmitStageStarted(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/started", DiagnosticSeverity.Info, null, fields);

    private static void EmitStageSucceeded(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/succeeded", DiagnosticSeverity.Info, null, fields);

    private static void EmitStageFailed(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/failed", DiagnosticSeverity.Error, message);

    private static void EmitStageSkipped(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/skipped", DiagnosticSeverity.Info, message);

    private static void EmitDiagnosticsRecord(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        string name,
        DiagnosticSeverity severity,
        string? message,
        DiagnosticFields? fields = null)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: stage,
            Name: name,
            Severity: severity,
            Message: message,
            Context: null,
            Fields: fields ?? DiagnosticFields.Empty,
            Timestamp: DateTimeOffset.UtcNow));
    }
}
