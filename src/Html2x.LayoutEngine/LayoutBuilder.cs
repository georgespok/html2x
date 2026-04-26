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
using Html2x.LayoutEngine.Pipeline;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

/// <summary>
/// Coordinates the deterministic HTML layout pipeline from DOM and style resolution
/// through display tree, geometry, fragment projection, pagination, and layout assembly.
/// </summary>
public class LayoutBuilder
{
    private readonly IBoxTreeBuilder _boxBuilder;
    private readonly IDomProvider _domProvider;
    private readonly IFragmentBuilder _fragmentBuilder;
    private readonly IStyleComputer _styleComputer;
    private readonly IImageProvider _imageProvider;
    private readonly ITextMeasurer _textMeasurer;
    private readonly IFontSource _fontSource;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public LayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider,
        ITextMeasurer textMeasurer,
        IFontSource fontSource)
        : this(
            domProvider,
            styleComputer,
            boxBuilder,
            fragmentBuilder,
            imageProvider,
            textMeasurer,
            fontSource,
            new BlockFormattingContext())
    {
    }

    internal LayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider,
        ITextMeasurer textMeasurer,
        IFontSource fontSource,
        IBlockFormattingContext blockFormattingContext)
    {
        _domProvider = domProvider ?? throw new ArgumentNullException(nameof(domProvider));
        _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
        _boxBuilder = boxBuilder ?? throw new ArgumentNullException(nameof(boxBuilder));
        _fragmentBuilder = fragmentBuilder ?? throw new ArgumentNullException(nameof(fragmentBuilder));
        _imageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
        _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
        _fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }
    
    public async Task<HtmlLayout> BuildAsync(string html, 
        LayoutOptions options, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var dom = await RunStageAsync("stage/dom", () => _domProvider.LoadAsync(html, options), diagnosticsSession);
        
        var styleStage = new StyleStageResult(
            RunStage("stage/style", () => _styleComputer.Compute(dom, diagnosticsSession), diagnosticsSession));

        var displayTreeStage = new DisplayTreeStageResult(RunStage(
            "stage/display-tree",
            () => _boxBuilder.BuildDisplayTree(styleStage.Tree, diagnosticsSession),
            diagnosticsSession));

        var layoutGeometryStage = new LayoutGeometryStageResult(RunStage("stage/layout-geometry", () => _boxBuilder.BuildLayoutGeometry(
            displayTreeStage.Root,
            styleStage.Tree,
            diagnosticsSession,
            new BoxTreeBuildContext(
                _imageProvider,
                options.HtmlDirectory,
                options.MaxImageSizeBytes)), diagnosticsSession));
        RunStage("stage/layout-validation", () => LayoutSnapshotMapper.ValidateInlineBlockStructures(layoutGeometryStage.Tree, diagnosticsSession), diagnosticsSession);

        var fragmentStage = new FragmentStageResult(RunStage("stage/fragment-projection", () => _fragmentBuilder.Build(
            layoutGeometryStage.Tree,
            new FragmentBuildContext(
                _imageProvider,
                options.HtmlDirectory,
                options.MaxImageSizeBytes,
                _textMeasurer,
                _fontSource,
                _blockFormattingContext)), diagnosticsSession));
        
        var layoutAssemblyStage = new LayoutAssemblyStageResult(RunStage(
            "stage/pagination",
            () => CreateHtmlLayout(options, layoutGeometryStage.Tree, fragmentStage.Tree, diagnosticsSession),
            diagnosticsSession));
        return layoutAssemblyStage.Layout;
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutOptions options,
        Models.BoxTree boxTree,
        FragmentTree fragments,
        DiagnosticsSession? diagnosticsSession)
    {
        var paginator = new BlockPaginator();
        var paginationStage = new PaginationStageResult(
            paginator.Paginate(fragments.Blocks, options.PageSize, boxTree.Page.Margin, diagnosticsSession));
        var layout = CreateHtmlLayout(paginationStage.Result);
        PublishGeometrySnapshot(boxTree, layout, paginationStage.Result, diagnosticsSession);
        return layout;
    }

    private static HtmlLayout CreateHtmlLayout(PaginationResult pagination)
    {
        var layout = new HtmlLayout();
        foreach (var page in pagination.Pages)
        {
            layout.Pages.Add(new LayoutPage(
                page.PageSize,
                page.Margins,
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


