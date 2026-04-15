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
///     High-level orchestrator that enforces the layered pipeline:
///     DOM/CSSOM -> Style Tree -> Box Tree -> Fragment Tree -> HtmlLayout
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
        
        var styleTree = RunStage("stage/style", () => _styleComputer.Compute(dom, diagnosticsSession), diagnosticsSession);

        var boxTree = RunStage("stage/layout", () => _boxBuilder.Build(styleTree, diagnosticsSession), diagnosticsSession);
        RunStage("stage/layout-validation", () => LayoutSnapshotMapper.ValidateInlineBlockStructures(boxTree, diagnosticsSession), diagnosticsSession);
        
        var fragments = RunStage("stage/inline-measurement", () => _fragmentBuilder.Build(
            boxTree,
            new FragmentBuildContext(
                _imageProvider,
                options.HtmlDirectory,
                options.MaxImageSizeBytes,
                _textMeasurer,
                _fontSource,
                _blockFormattingContext)), diagnosticsSession);

        RunStage("stage/fragmentation", () =>
        {
            // Fragmentation stage currently aligns with fragment building; placeholder for future expansion.
        }, diagnosticsSession);
        
        return RunStage("stage/pagination", () => CreateHtmlLayout(options, boxTree, fragments, diagnosticsSession), diagnosticsSession);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutOptions options,
        Models.BoxTree boxTree,
        FragmentTree fragments,
        DiagnosticsSession? diagnosticsSession)
    {
        var paginator = new BlockPaginator();
        var pagination = paginator.Paginate(fragments.Blocks, options.PageSize, boxTree.Page.Margin, diagnosticsSession);
        return CreateHtmlLayout(pagination);
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


