using AngleSharp;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

/// <summary>
/// Coordinates the deterministic HTML layout pipeline from DOM and style resolution
/// through box tree layout, fragment projection, pagination, and layout assembly.
/// </summary>
public class LayoutBuilder
{
    private readonly BoxTreeBuilder _boxBuilder;
    private readonly AngleSharpDomProvider _domProvider;
    private readonly FragmentBuilder _fragmentBuilder;
    private readonly CssStyleComputer _styleComputer;
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
        var angleSharpConfig = Configuration.Default.WithCss();

        _domProvider = new AngleSharpDomProvider(angleSharpConfig);
        _styleComputer = new CssStyleComputer();
        _boxBuilder = new BoxTreeBuilder(textMeasurer, blockFormattingContext);
        _fragmentBuilder = new FragmentBuilder();
        _imageProvider = imageProvider;
        _fontSource = fontSource;
    }

    public async Task<HtmlLayout> BuildAsync(string html, 
        LayoutOptions options, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var dom = await RunStageAsync("stage/dom", () => _domProvider.LoadAsync(html, options), diagnosticsSession);
        
        var styleTree = RunStage("stage/style", () => _styleComputer.Compute(dom, diagnosticsSession), diagnosticsSession);

        var boxTree = RunStage("stage/box-tree", () => _boxBuilder.Build(
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
            boxTree,
            _fontSource), diagnosticsSession);
        
        return RunStage(
            "stage/pagination",
            () => CreateHtmlLayout(options, boxTree, fragments, diagnosticsSession),
            diagnosticsSession);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutOptions options,
        Models.BoxTree boxTree,
        FragmentTree fragments,
        DiagnosticsSession? diagnosticsSession)
    {
        var paginator = new BlockPaginator();
        var pagination = paginator.Paginate(fragments.Blocks, options.PageSize, boxTree.Page.Margin, diagnosticsSession);
        var layout = CreateHtmlLayout(pagination);
        PublishGeometrySnapshot(boxTree, layout, pagination, diagnosticsSession);
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
        Models.BoxTree boxTree,
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
                Snapshot = GeometrySnapshotMapper.From(boxTree, layout, pagination)
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

    private static async Task<T> RunStageAsync<T>(
        string stage,
        Func<Task<T>> action,
        DiagnosticsSession? diagnosticsSession = null)
    {
        diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageStarted(stage));
        try
        {
            var result = await action();
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageSucceeded(stage));
            return result;
        }
        catch (Exception exception)
        {
            diagnosticsSession?.Events.Add(DiagnosticsEventFactory.StageFailed(stage, exception.Message));
            throw;
        }
    }
}


