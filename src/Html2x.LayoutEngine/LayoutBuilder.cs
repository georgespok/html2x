using Html2x.Diagnostics.Contracts;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;

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
    private readonly IImageProvider _imageProvider;
    private readonly IFontSource _fontSource;

    public LayoutBuilder(
        ITextMeasurer textMeasurer,
        IFontSource fontSource,
        IImageProvider imageProvider)
    {
        ArgumentNullException.ThrowIfNull(textMeasurer);
        ArgumentNullException.ThrowIfNull(fontSource);
        ArgumentNullException.ThrowIfNull(imageProvider);

        var blockFormattingContext = new BlockFormattingContext();

        _styleTreeBuilder = new StyleTreeBuilder();
        _layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer, blockFormattingContext);
        _fragmentBuilder = new FragmentBuilder();
        _imageProvider = imageProvider;
        _fontSource = fontSource;
    }

    public async Task<HtmlLayout> BuildAsync(
        string html,
        LayoutOptions options,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var styleTree = await _styleTreeBuilder.BuildAsync(html, options, diagnosticsSink: diagnosticsSink);

        var publishedLayout = RunStage("stage/box-tree", () => _layoutGeometryBuilder.Build(
            styleTree,
            new LayoutGeometryRequest
            {
                PageSize = options.PageSize,
                ImageProvider = _imageProvider,
                HtmlDirectory = options.HtmlDirectory,
                MaxImageSizeBytes = options.MaxImageSizeBytes
            },
            diagnosticsSink), diagnosticsSink);
        var fragments = RunStage("stage/fragment-tree", () => _fragmentBuilder.Build(
            publishedLayout,
            _fontSource), diagnosticsSink);
        
        return RunStage(
            "stage/pagination",
            () => CreateHtmlLayout(options, publishedLayout, fragments, diagnosticsSink),
            diagnosticsSink);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutOptions options,
        PublishedLayoutTree publishedLayout,
        FragmentTree fragments,
        IDiagnosticsSink? diagnosticsSink)
    {
        var paginator = new BlockPaginator();
        var pagination = paginator.Paginate(
            fragments.Blocks,
            options.PageSize,
            publishedLayout.Page.Margin,
            diagnosticsSink);
        var layout = CreateHtmlLayout(pagination);
        PublishGeometrySnapshot(publishedLayout, layout, pagination, diagnosticsSink);
        return layout;
    }

    private static HtmlLayout CreateHtmlLayout(PaginationResult pagination)
    {
        var layout = new HtmlLayout();
        foreach (var page in pagination.Pages)
        {
            layout.Pages.Add(new LayoutPage(
                page.PageSize,
                page.Margin,
                CreateLayoutPageChildren(page),
                page.PageNumber));
        }

        return layout;
    }

    private static IReadOnlyList<Html2x.Abstractions.Layout.Fragments.Fragment> CreateLayoutPageChildren(PageModel page)
    {
        return page.Placements
            .Select(static placement => (Html2x.Abstractions.Layout.Fragments.Fragment)placement.Fragment)
            .ToList();
    }

    private static void PublishGeometrySnapshot(
        PublishedLayoutTree publishedLayout,
        HtmlLayout layout,
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
                    GeometrySnapshotMapper.ToDiagnosticObject(publishedLayout, layout, pagination))),
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


