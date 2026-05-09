using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.Options;
using Html2x.Renderers.Pdf;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.RenderModel.Documents;
using Html2x.Resources;
using Html2x.Text;

namespace Html2x;

public sealed class HtmlConverter
{
    private readonly HtmlConverterRuntime _runtime;

    public HtmlConverter()
        : this(HtmlConverterRuntime.Default)
    {
    }

    public HtmlConverter(HtmlConverterRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public async Task<Html2PdfResult> ToPdfAsync(
        string html,
        HtmlConverterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        options ??= new();

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

        using var ownedTextMeasurer = _runtime.TextMeasurer is null
            ? CreateTextMeasurer(options, collector, diagnosticsSink)
            : null;
        var textMeasurer = _runtime.TextMeasurer
                           ?? ownedTextMeasurer
                           ?? throw new InvalidOperationException("Text measurer could not be resolved.");

        var imageResources = new ConversionImageResourceStore(baseDirectory, options.Resources.MaxImageSizeBytes);
        var imageMetadataResolver = new ImageResourceMetadataResolver(imageResources);

        DiagnosticStageEmitter.Started(
            diagnosticsSink,
            FacadeDiagnosticNames.Stages.LayoutBuild,
            CreateLayoutStartFields(html, options.Diagnostics));

        var layoutBuilder = new LayoutBuilder(textMeasurer, imageMetadataResolver);

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
            DiagnosticStageEmitter.Cancelled(
                diagnosticsSink,
                FacadeDiagnosticNames.Stages.LayoutBuild,
                "LayoutBuild canceled.");
            DiagnosticStageEmitter.Skipped(
                diagnosticsSink,
                FacadeDiagnosticNames.Stages.PdfRender,
                "Skipped because LayoutBuild was canceled.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, FacadeDiagnosticNames.Stages.LayoutBuild, exception.Message);
            DiagnosticStageEmitter.Skipped(
                diagnosticsSink,
                FacadeDiagnosticNames.Stages.PdfRender,
                FacadeDiagnosticNames.Messages.SkippedBecauseLayoutBuildFailed);
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        DiagnosticStageEmitter.Succeeded(
            diagnosticsSink,
            FacadeDiagnosticNames.Stages.LayoutBuild,
            DiagnosticFields.Create(
                DiagnosticFields.Field(
                    FacadeDiagnosticNames.Fields.Snapshot,
                    LayoutSnapshotMapper.ToDiagnosticObject(layout))));

        var renderer = new PdfRenderer();

        DiagnosticStageEmitter.Started(diagnosticsSink, FacadeDiagnosticNames.Stages.PdfRender);

        byte[] pdfBytes;
        try
        {
            pdfBytes = await renderer.RenderAsync(
                layout,
                ToPdfRenderSettings(options, baseDirectory, imageResources),
                diagnosticsSink,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Cancelled(
                diagnosticsSink,
                FacadeDiagnosticNames.Stages.PdfRender,
                "PdfRender canceled.");
            AttachDiagnosticsReport(exception, collector);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(diagnosticsSink, FacadeDiagnosticNames.Stages.PdfRender, exception.Message);
            AttachDiagnosticsReport(exception, collector);
            throw;
        }

        DiagnosticStageEmitter.Succeeded(
            diagnosticsSink,
            FacadeDiagnosticNames.Stages.PdfRender,
            DiagnosticFields.Create(
                DiagnosticFields.Field(FacadeDiagnosticNames.Fields.PdfSize, pdfBytes.Length),
                DiagnosticFields.Field(FacadeDiagnosticNames.Fields.PageCount, layout.Pages.Count)));

        var report = CompleteDiagnostics(collector);

        return new(pdfBytes)
        {
            DiagnosticsReport = report
        };

        SkiaTextMeasurer CreateTextMeasurer(
            HtmlConverterOptions converterOptions,
            DiagnosticsCollector? activeCollector,
            IDiagnosticsSink? activeDiagnosticsSink)
        {
            var fontSource = ResolveFontSource(converterOptions, activeCollector);
            if (activeDiagnosticsSink is not null)
            {
                fontSource = new DiagnosticsFontSource(fontSource, activeDiagnosticsSink);
            }

            return new(fontSource);
        }
    }

    private static LayoutBuildSettings ToLayoutBuildSettings(HtmlConverterOptions options, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new()
        {
            PageSize = options.Page.Size,
            ResourceBaseDirectory = baseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes,
            Style = new()
            {
                UseDefaultUserAgentStyleSheet = options.Css.UseDefaultUserAgentStyleSheet,
                UserAgentStyleSheet = options.Css.UserAgentStyleSheet
            }
        };
    }

    private static PdfRenderSettings ToPdfRenderSettings(
        HtmlConverterOptions options,
        string baseDirectory,
        IImageResourceReader imageResources)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(imageResources);

        return new()
        {
            ResourceBaseDirectory = baseDirectory,
            MaxImageSizeBytes = options.Resources.MaxImageSizeBytes,
            ImageResources = imageResources
        };
    }

    private IFontSource ResolveFontSource(
        HtmlConverterOptions options,
        DiagnosticsCollector? collector)
    {
        if (_runtime.FontSource is { } runtimeFontSource)
        {
            return runtimeFontSource;
        }

        var fontPath = options.Fonts.FontPath;
        if (string.IsNullOrWhiteSpace(fontPath))
        {
            throw CreateFontPathException(
                "HtmlConverterOptions.Fonts.FontPath must be provided before layout can begin.",
                collector);
        }

        try
        {
            return new FontPathSource(fontPath);
        }
        catch (FontResolutionException)
        {
            throw CreateFontPathException(
                $"HtmlConverterOptions.Fonts.FontPath '{fontPath}' does not exist.",
                collector);
        }
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
            DiagnosticFields.Field(FacadeDiagnosticNames.Fields.HtmlLength, html.Length)
        };

        if (diagnosticsOptions.IncludeRawHtml)
        {
            var rawHtml = html.Trim();
            fields.Add(DiagnosticFields.Field(
                FacadeDiagnosticNames.Fields.Html,
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength
                    ? rawHtml[..diagnosticsOptions.MaxRawHtmlLength]
                    : rawHtml));
            fields.Add(DiagnosticFields.Field(
                FacadeDiagnosticNames.Fields.HtmlTruncated,
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength));
        }

        return new(fields);
    }

    private static InvalidOperationException CreateFontPathException(
        string message,
        DiagnosticsCollector? collector)
    {
        IDiagnosticsSink? diagnosticsSink = collector;
        DiagnosticStageEmitter.Emit(
            diagnosticsSink,
            FacadeDiagnosticNames.Stages.Configuration,
            FacadeDiagnosticNames.Events.FontPathError,
            DiagnosticSeverity.Error,
            message);
        DiagnosticStageEmitter.Failed(diagnosticsSink, FacadeDiagnosticNames.Stages.LayoutBuild, message);
        DiagnosticStageEmitter.Skipped(
            diagnosticsSink,
            FacadeDiagnosticNames.Stages.PdfRender,
            FacadeDiagnosticNames.Messages.SkippedBecauseLayoutBuildFailed);

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
            exception.Data[nameof(Html2PdfResult.DiagnosticsReport)] = report;
        }
    }

    private static DiagnosticsReport? CompleteDiagnostics(DiagnosticsCollector? collector)
    {
        var endTime = DateTimeOffset.UtcNow;
        return collector?.ToReport(endTime);
    }

    private static class FacadeDiagnosticNames
    {
        public static class Stages
        {
            public const string LayoutBuild = "LayoutBuild";
            public const string PdfRender = "PdfRender";
            public const string Configuration = "Configuration";
        }

        public static class Events
        {
            public const string FontPathError = "font-path/error";
        }

        public static class Fields
        {
            public const string Snapshot = "snapshot";
            public const string PdfSize = "pdfSize";
            public const string PageCount = "pageCount";
            public const string HtmlLength = "htmlLength";
            public const string Html = "html";
            public const string HtmlTruncated = "htmlTruncated";
        }

        public static class Messages
        {
            public const string SkippedBecauseLayoutBuildFailed = "Skipped because LayoutBuild failed.";
        }
    }
}
