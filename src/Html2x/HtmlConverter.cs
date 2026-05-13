using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.Options;
using Html2x.Renderers.Pdf.Pipeline;
using Html2x.RenderModel.Documents;

namespace Html2x;

public sealed class HtmlConverter(HtmlConverterDependencies dependencies)
{
    private readonly HtmlConverterDependencies _dependencies = 
        dependencies ?? throw new ArgumentNullException(nameof(dependencies));

    public HtmlConverter()
        : this(HtmlConverterDependencies.Default)
    {
    }

    public async Task<HtmlToPdfResult> ToPdfAsync(
        string html,
        HtmlConverterOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(html);

        options ??= new();
        HtmlConverterOptionsValidator.Validate(options);
        var baseDirectory = HtmlConverterOptionsValidator.ResolveExistingBaseDirectory(options);
        var diagnostics = HtmlConversionDiagnostics.Create(options.Diagnostics);

        using var resources = CreateConversionResources(
            _dependencies,
            options,
            baseDirectory,
            diagnostics);

        DiagnosticStageEmitter.Started(
            diagnostics.Sink,
            HtmlConverterDiagnosticNames.Stages.LayoutBuild,
            diagnostics.CreateLayoutStartFields(html, options.Diagnostics));

        var layoutPipeline = new LayoutPipeline(resources.TextMeasurer, resources.ImageMetadataResolver);
        var layout = await BuildLayoutAsync(
            layoutPipeline,
            html,
            options,
            diagnostics,
            cancellationToken);

        DiagnosticStageEmitter.Succeeded(
            diagnostics.Sink,
            HtmlConverterDiagnosticNames.Stages.LayoutBuild,
            DiagnosticFields.Create(
                DiagnosticFields.Field(
                    HtmlConverterDiagnosticNames.Fields.Snapshot,
                    LayoutSnapshotMapper.ToDiagnosticObject(layout))));

        var renderer = new PdfRenderer();

        DiagnosticStageEmitter.Started(diagnostics.Sink, HtmlConverterDiagnosticNames.Stages.PdfRender);

        var pdfBytes = RenderPdf(
            renderer,
            layout,
            options,
            baseDirectory,
            resources,
            diagnostics,
            cancellationToken);

        DiagnosticStageEmitter.Succeeded(
            diagnostics.Sink,
            HtmlConverterDiagnosticNames.Stages.PdfRender,
            DiagnosticFields.Create(
                DiagnosticFields.Field(HtmlConverterDiagnosticNames.Fields.PdfSize, pdfBytes.Length),
                DiagnosticFields.Field(HtmlConverterDiagnosticNames.Fields.PageCount, layout.Pages.Count)));

        return new(pdfBytes)
        {
            DiagnosticsReport = diagnostics.Complete()
        };
    }

    private static async Task<HtmlLayout> BuildLayoutAsync(
        LayoutPipeline layoutPipeline,
        string html,
        HtmlConverterOptions options,
        HtmlConversionDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await layoutPipeline.BuildAsync(
                html,
                HtmlConverterSettingsMapper.ToLayoutBuildSettings(options),
                diagnostics.Sink,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Canceled(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.LayoutBuild,
                "LayoutBuild canceled.");
            DiagnosticStageEmitter.Skipped(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.PdfRender,
                "Skipped because LayoutBuild was canceled.");
            diagnostics.AttachReportTo(exception);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.LayoutBuild,
                exception.Message);
            DiagnosticStageEmitter.Skipped(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.PdfRender,
                HtmlConverterDiagnosticNames.Messages.SkippedBecauseLayoutBuildFailed);
            diagnostics.AttachReportTo(exception);
            throw;
        }
    }

    private static ConversionResources CreateConversionResources(
        HtmlConverterDependencies dependencies,
        HtmlConverterOptions options,
        string baseDirectory,
        HtmlConversionDiagnostics diagnostics)
    {
        try
        {
            return ConversionResources.Create(
                dependencies,
                options,
                baseDirectory,
                diagnostics);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException &&
            !exception.Data.Contains(nameof(HtmlToPdfResult.DiagnosticsReport)))
        {
            diagnostics.AttachConfigurationFailure(exception);
            throw;
        }
    }

    private static byte[] RenderPdf(
        PdfRenderer renderer,
        HtmlLayout layout,
        HtmlConverterOptions options,
        string baseDirectory,
        ConversionResources resources,
        HtmlConversionDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            return renderer.Render(
                layout,
                HtmlConverterSettingsMapper.ToPdfRenderSettings(options, baseDirectory, resources.ImageResources),
                diagnostics.Sink,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            DiagnosticStageEmitter.Canceled(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.PdfRender,
                "PdfRender canceled.");
            diagnostics.AttachReportTo(exception);
            throw;
        }
        catch (Exception exception)
        {
            DiagnosticStageEmitter.Failed(
                diagnostics.Sink,
                HtmlConverterDiagnosticNames.Stages.PdfRender,
                exception.Message);
            diagnostics.AttachReportTo(exception);
            throw;
        }
    }
}
