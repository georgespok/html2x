using Html2x.Diagnostics;
using Html2x.Diagnostics.Contracts;
using Html2x.Options;

namespace Html2x;

internal sealed class HtmlConversionDiagnostics
{
    private readonly DiagnosticsCollector? _collector;

    private HtmlConversionDiagnostics(DiagnosticsCollector? collector)
    {
        _collector = collector;
        Sink = collector;
    }

    public IDiagnosticsSink? Sink { get; }

    public static HtmlConversionDiagnostics Create(DiagnosticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnableDiagnostics
            ? new(new DiagnosticsCollector(DateTimeOffset.UtcNow))
            : new(null);
    }

    public DiagnosticFields CreateLayoutStartFields(string html, DiagnosticsOptions diagnosticsOptions)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(diagnosticsOptions);

        var fields = new List<KeyValuePair<string, DiagnosticValue?>>
        {
            DiagnosticFields.Field(HtmlConverterDiagnosticNames.Fields.HtmlLength, html.Length)
        };

        if (diagnosticsOptions.IncludeRawHtml)
        {
            var rawHtml = html.Trim();
            fields.Add(DiagnosticFields.Field(
                HtmlConverterDiagnosticNames.Fields.Html,
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength
                    ? rawHtml[..diagnosticsOptions.MaxRawHtmlLength]
                    : rawHtml));
            fields.Add(DiagnosticFields.Field(
                HtmlConverterDiagnosticNames.Fields.HtmlTruncated,
                rawHtml.Length > diagnosticsOptions.MaxRawHtmlLength));
        }

        return new(fields);
    }

    public InvalidOperationException CreateFontPathException(string message)
    {
        DiagnosticStageEmitter.Emit(
            Sink,
            HtmlConverterDiagnosticNames.Stages.Configuration,
            HtmlConverterDiagnosticNames.Events.FontPathError,
            DiagnosticSeverity.Error,
            message);
        DiagnosticStageEmitter.Failed(Sink, HtmlConverterDiagnosticNames.Stages.Configuration, message);
        DiagnosticStageEmitter.Skipped(
            Sink,
            HtmlConverterDiagnosticNames.Stages.LayoutBuild,
            HtmlConverterDiagnosticNames.Messages.SkippedBecauseConfigurationFailed);
        DiagnosticStageEmitter.Skipped(
            Sink,
            HtmlConverterDiagnosticNames.Stages.PdfRender,
            HtmlConverterDiagnosticNames.Messages.SkippedBecauseConfigurationFailed);

        var exception = new InvalidOperationException(message);
        AttachReportTo(exception);
        return exception;
    }

    public void AttachConfigurationFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        DiagnosticStageEmitter.Failed(
            Sink,
            HtmlConverterDiagnosticNames.Stages.Configuration,
            exception.Message);
        DiagnosticStageEmitter.Skipped(
            Sink,
            HtmlConverterDiagnosticNames.Stages.LayoutBuild,
            HtmlConverterDiagnosticNames.Messages.SkippedBecauseConfigurationFailed);
        DiagnosticStageEmitter.Skipped(
            Sink,
            HtmlConverterDiagnosticNames.Stages.PdfRender,
            HtmlConverterDiagnosticNames.Messages.SkippedBecauseConfigurationFailed);
        AttachReportTo(exception);
    }

    public void AttachReportTo(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var report = Complete();
        if (report is not null)
        {
            exception.Data[nameof(HtmlToPdfResult.DiagnosticsReport)] = report;
        }
    }

    public DiagnosticsReport? Complete()
    {
        var endTime = DateTimeOffset.UtcNow;
        return _collector?.ToReport(endTime);
    }
}
