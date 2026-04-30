using Html2x.Abstractions.Diagnostics;
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

    public async Task<HtmlLayout> BuildAsync(string html, 
        LayoutOptions options, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var styleTree = await _styleTreeBuilder.BuildAsync(html, options, diagnosticsSession);

        var publishedLayout = RunStage("stage/box-tree", () => _layoutGeometryBuilder.Build(
            styleTree,
            diagnosticsSession,
            new LayoutGeometryRequest
            {
                PageSize = options.PageSize,
                ImageProvider = _imageProvider,
                HtmlDirectory = options.HtmlDirectory,
                MaxImageSizeBytes = options.MaxImageSizeBytes
            }), diagnosticsSession);
        var fragments = RunStage("stage/fragment-tree", () => _fragmentBuilder.Build(
            publishedLayout,
            _fontSource), diagnosticsSession);
        
        return RunStage(
            "stage/pagination",
            () => CreateHtmlLayout(options, publishedLayout, fragments, diagnosticsSession),
            diagnosticsSession);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutOptions options,
        PublishedLayoutTree publishedLayout,
        FragmentTree fragments,
        DiagnosticsSession? diagnosticsSession)
    {
        var paginator = new BlockPaginator();
        var pagination = paginator.Paginate(
            fragments.Blocks,
            options.PageSize,
            publishedLayout.Page.Margin,
            diagnosticsSession);
        var layout = CreateHtmlLayout(pagination);
        PublishGeometrySnapshot(publishedLayout, layout, pagination, diagnosticsSession);
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
        DiagnosticsSession? diagnosticsSession)
    {
        if (diagnosticsSession is null)
        {
            return;
        }

        diagnosticsSession.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "layout/geometry-snapshot",
            Severity = DiagnosticSeverity.Info,
            Payload = new GeometrySnapshotPayload
            {
                Snapshot = GeometrySnapshotMapper.From(publishedLayout, layout, pagination)
            }
        });
    }

    private static T RunStage<T>(string stage, Func<T> action, DiagnosticsSession? diagnosticsSession = null)
    {
        diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageStarted(stage));
        try
        {
            var result = action();
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageSucceeded(stage));
            return result;
        }
        catch (Exception exception)
        {
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageFailed(stage, exception.Message));
            throw;
        }
    }

    private static void RunStage(string stage, Action action, DiagnosticsSession? diagnosticsSession = null)
    {
        RunStage<object?>(stage, () =>
        {
            action();
            return null;
        }, diagnosticsSession);
    }

}


