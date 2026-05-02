using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine;

/// <summary>
/// Coordinates the deterministic HTML layout pipeline from DOM and style resolution
/// through box tree layout, fragment projection, pagination, and layout assembly.
/// </summary>
public class LayoutBuilder
{
    private readonly LayoutGeometryBuilder _layoutGeometryBuilder;
    private readonly FragmentBuilder _fragmentBuilder;
    private readonly IStyleTreeBuilder _styleTreeBuilder;
    private readonly IImageMetadataResolver _imageMetadataResolver;

    public LayoutBuilder(
        ITextMeasurer textMeasurer,
        IImageMetadataResolver imageMetadataResolver)
    {
        ArgumentNullException.ThrowIfNull(textMeasurer);
        ArgumentNullException.ThrowIfNull(imageMetadataResolver);

        var blockFormattingContext = new BlockFormattingContext();

        _styleTreeBuilder = new StyleTreeBuilder();
        _layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer, blockFormattingContext);
        _fragmentBuilder = new FragmentBuilder();
        _imageMetadataResolver = imageMetadataResolver;
    }

    public async Task<HtmlLayout> BuildAsync(
        string html,
        LayoutBuildSettings settings,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(settings);

        var styleTree = await _styleTreeBuilder.BuildAsync(html, settings.Style, diagnosticsSink: diagnosticsSink);

        var publishedLayout = RunStage("stage/box-tree", () => _layoutGeometryBuilder.Build(
            styleTree,
            new LayoutGeometryRequest
            {
                PageSize = settings.PageSize,
                ImageMetadataResolver = _imageMetadataResolver,
                HtmlDirectory = settings.HtmlDirectory,
                MaxImageSizeBytes = settings.MaxImageSizeBytes
            },
            diagnosticsSink), diagnosticsSink);
        var fragments = RunStage(
            "stage/fragment-tree",
            () => _fragmentBuilder.Build(publishedLayout),
            diagnosticsSink);
        
        return RunStage(
            "stage/pagination",
            () => CreateHtmlLayout(settings, publishedLayout, fragments, diagnosticsSink),
            diagnosticsSink);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutBuildSettings settings,
        PublishedLayoutTree publishedLayout,
        FragmentTree fragments,
        IDiagnosticsSink? diagnosticsSink)
    {
        var paginator = new LayoutPaginator();
        var pagination = paginator.Paginate(
            fragments.Blocks,
            new PaginationOptions
            {
                PageSize = settings.PageSize,
                Margin = publishedLayout.Page.Margin
            },
            diagnosticsSink);
        PublishGeometrySnapshot(publishedLayout, pagination, diagnosticsSink);
        return pagination.Layout;
    }

    private static void PublishGeometrySnapshot(
        PublishedLayoutTree publishedLayout,
        PaginationResult pagination,
        IDiagnosticsSink? diagnosticsSink)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/pagination",
            Name: "layout/geometry-snapshot",
            Severity: DiagnosticSeverity.Info,
            Message: null,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(
                    "snapshot",
                    GeometrySnapshotMapper.ToDiagnosticObject(publishedLayout, pagination))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static T RunStage<T>(
        string stage,
        Func<T> action,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        EmitStageStarted(diagnosticsSink, stage);
        try
        {
            var result = action();
            EmitStageSucceeded(diagnosticsSink, stage);
            return result;
        }
        catch (Exception exception)
        {
            EmitStageFailed(diagnosticsSink, stage, exception.Message);
            throw;
        }
    }

    private static void RunStage(
        string stage,
        Action action,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        RunStage<object?>(stage, () =>
        {
            action();
            return null;
        }, diagnosticsSink);
    }

    private static void EmitStageStarted(IDiagnosticsSink? diagnosticsSink, string stage) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/started", DiagnosticSeverity.Info, null);

    private static void EmitStageSucceeded(IDiagnosticsSink? diagnosticsSink, string stage) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/succeeded", DiagnosticSeverity.Info, null);

    private static void EmitStageFailed(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        EmitDiagnosticsRecord(diagnosticsSink, stage, "stage/failed", DiagnosticSeverity.Error, message);

    private static void EmitDiagnosticsRecord(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        string name,
        DiagnosticSeverity severity,
        string? message)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: stage,
            Name: name,
            Severity: severity,
            Message: message,
            Context: null,
            Fields: DiagnosticFields.Empty,
            Timestamp: DateTimeOffset.UtcNow));
    }

}
