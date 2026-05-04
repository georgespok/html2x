using Html2x.RenderModel;
using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Style;
using Html2x.Resources;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.Text;

namespace Html2x;

public class HtmlConverter
{
    public async Task<Html2PdfResult> ToPdfAsync(
        string html,
        HtmlConverterOptions? options = null,
        CancellationToken cancellationToken = default)
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

        ValidateOptions(options);
        var baseDirectory = ResolveBaseDirectory(options);

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
        catch (FontResolutionException)
        {
            throw CreateFontPathException(
                $"HtmlConverterOptions.Fonts.FontPath '{fontPath}' does not exist.",
                collector);
        }

        if (diagnosticsSink is not null)
        {
            fontSource = new DiagnosticsFontSource(fontSource, diagnosticsSink);
        }

        using var measurer = new SkiaTextMeasurer(fontSource);
        var imageMetadataResolver = new FileImageProvider();

        DiagnosticStageEmitter.Started(
            diagnosticsSink,
            "LayoutBuild",
            CreateLayoutStartFields(html, options.Diagnostics));

        var layoutBuilder = new LayoutBuilder(measurer, imageMetadataResolver);

        HtmlLayout layout;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            layout = await layoutBuilder.BuildAsync(
                html,
                ToLayoutBuildSettings(options, baseDirectory),
                diagnosticsSink,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Cancelled(diagnosticsSink, "LayoutBuild", "LayoutBuild canceled.");
            DiagnosticStageEmitter.Skipped(
                diagnosticsSink,
                "PdfRender",
                "Skipped because LayoutBuild was canceled.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, "LayoutBuild", exception.Message);
            DiagnosticStageEmitter.Skipped(diagnosticsSink, "PdfRender", "Skipped because LayoutBuild failed.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        DiagnosticStageEmitter.Succeeded(
            diagnosticsSink,
            "LayoutBuild",
            DiagnosticFields.Create(
                DiagnosticFields.Field("snapshot", LayoutSnapshotMapper.ToDiagnosticObject(layout))));

        var renderer = new PdfRenderer();

        DiagnosticStageEmitter.Started(diagnosticsSink, "PdfRender");

        byte[] pdfBytes;
        try
        {
            pdfBytes = await renderer.RenderAsync(
                layout,
                ToPdfRenderSettings(options, baseDirectory),
                diagnosticsSink: diagnosticsSink,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Cancelled(diagnosticsSink, "PdfRender", "PdfRender canceled.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, "PdfRender", exception.Message);
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        DiagnosticStageEmitter.Succeeded(
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

    private static LayoutBuildSettings ToLayoutBuildSettings(HtmlConverterOptions options, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new LayoutBuildSettings
        {
            PageSize = options.Page.Size,
            HtmlDirectory = baseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes,
            Style = new StyleBuildSettings
            {
                UseDefaultUserAgentStyleSheet = options.Css.UseDefaultUserAgentStyleSheet,
                UserAgentStyleSheet = options.Css.UserAgentStyleSheet
            }
        };
    }

    private static PdfRenderSettings ToPdfRenderSettings(HtmlConverterOptions options, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new PdfRenderSettings
        {
            HtmlDirectory = baseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes
        };
    }

    private static void ValidateOptions(HtmlConverterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Page);
        ArgumentNullException.ThrowIfNull(options.Resources);
        ArgumentNullException.ThrowIfNull(options.Css);
        ArgumentNullException.ThrowIfNull(options.Fonts);
        ArgumentNullException.ThrowIfNull(options.Diagnostics);

        if (options.Resources.MaxImageSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ResourceOptions.MaxImageSizeBytes),
                "HtmlConverterOptions.Resources.MaxImageSizeBytes must be greater than zero.");
        }

        if (options.Diagnostics.MaxRawHtmlLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DiagnosticsOptions.MaxRawHtmlLength),
                "HtmlConverterOptions.Diagnostics.MaxRawHtmlLength must be greater than zero.");
        }
    }

    private static string ResolveBaseDirectory(HtmlConverterOptions options)
    {
        var configuredBaseDirectory = options.Resources.BaseDirectory;
        var resolvedBaseDirectory = ImageResourceLoader.ResolveBaseDirectory(configuredBaseDirectory);
        if (!string.IsNullOrWhiteSpace(configuredBaseDirectory) &&
            !Directory.Exists(resolvedBaseDirectory))
        {
            throw new DirectoryNotFoundException(
                $"HtmlConverterOptions.Resources.BaseDirectory '{configuredBaseDirectory}' does not exist.");
        }

        return resolvedBaseDirectory;
    }

    private static DiagnosticFields CreateLayoutStartFields(
        string html,
        DiagnosticsOptions diagnosticsOptions)
    {
        var fields = new List<KeyValuePair<string, DiagnosticValue?>>
        {
            DiagnosticFields.Field("htmlLength", html.Length)
        };

        if (diagnosticsOptions.IncludeRawHtml)
        {
            var rawHtml = html.Trim();
            fields.Add(DiagnosticFields.Field(
                "html",
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength
                    ? rawHtml[..diagnosticsOptions.MaxRawHtmlLength]
                    : rawHtml));
            fields.Add(DiagnosticFields.Field(
                "htmlTruncated",
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength));
        }

        return new DiagnosticFields(fields);
    }

    private static InvalidOperationException CreateFontPathException(
        string message,
        DiagnosticsCollector? collector)
    {
        IDiagnosticsSink? diagnosticsSink = collector;
        DiagnosticStageEmitter.Emit(
            diagnosticsSink,
            "Configuration",
            "font-path/error",
            DiagnosticSeverity.Error,
            message);
        DiagnosticStageEmitter.Failed(diagnosticsSink, "LayoutBuild", message);
        DiagnosticStageEmitter.Skipped(diagnosticsSink, "PdfRender", "Skipped because LayoutBuild failed.");

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
}
