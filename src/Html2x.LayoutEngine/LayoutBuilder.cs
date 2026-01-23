using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

/// <summary>
///     High-level orchestrator that enforces the layered pipeline:
///     DOM/CSSOM -> Style Tree -> Box Tree -> Fragment Tree -> HtmlLayout
/// </summary>
public class LayoutBuilder(
    IDomProvider domProvider,
    IStyleComputer styleComputer,
    IBoxTreeBuilder boxBuilder,
    IFragmentBuilder fragmentBuilder,
    IImageProvider imageProvider,
    ITextMeasurer textMeasurer,
    IFontSource fontSource)
{
    private readonly IBoxTreeBuilder _boxBuilder = boxBuilder ?? throw new ArgumentNullException(nameof(boxBuilder));
    private readonly IDomProvider _domProvider = domProvider ?? throw new ArgumentNullException(nameof(domProvider));
    private readonly IFragmentBuilder _fragmentBuilder = fragmentBuilder ?? throw new ArgumentNullException(nameof(fragmentBuilder));
    private readonly IStyleComputer _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
    private readonly IImageProvider _imageProvider = imageProvider ?? throw new ArgumentNullException(nameof(imageProvider));
    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    private readonly IFontSource _fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
    
    public async Task<HtmlLayout> BuildAsync(string html, 
        LayoutOptions options, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var dom = await RunStage("stage/dom", async () => await _domProvider.LoadAsync(html, options));
        
        var styleTree = RunStage("stage/style", () => _styleComputer.Compute(dom));

        var boxTree = RunStage("stage/layout", () => _boxBuilder.Build(styleTree, diagnosticsSession));
        
        var fragments = RunStage("stage/inline-measurement", () => _fragmentBuilder.Build(
            boxTree,
            new FragmentBuildContext(
                _imageProvider,
                options.HtmlDirectory,
                options.MaxImageSizeBytes,
                _textMeasurer,
                _fontSource)));

        RunStage("stage/fragmentation", () =>
        {
            // Fragmentation stage currently aligns with fragment building; placeholder for future expansion.
        });
        
        var layout = RunStage("stage/pagination", () => CreateHtmlLayout(options, boxTree, fragments));

        return layout;
    }

    private static HtmlLayout CreateHtmlLayout(LayoutOptions options, Models.BoxTree boxTree, FragmentTree fragments)
    {
        var newLayout = new HtmlLayout();
        var pageSize = options.PageSize;
        var page = new LayoutPage(pageSize,
            boxTree.Page.Margin,
            fragments.Blocks);
        newLayout.Pages.Add(page);
        return newLayout;
    }

    private static void PublishEvent(DiagnosticsSession? diagnosticsSession, DiagnosticsEventType eventType, string eventName, string? eventDescription = null) =>
        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = eventType,
            Name = eventName,
            Description = eventDescription
        });

    private T RunStage<T>(string stage, Func<T> action, DiagnosticsSession? diagnosticsSession = null)
    {
        var result = action();
        PublishEvent(diagnosticsSession, DiagnosticsEventType.EndStage, stage);
        return result;
    }

    private void RunStage(string stage, Action action, DiagnosticsSession? diagnosticsSession = null)
    {
        action();
        PublishEvent(diagnosticsSession, DiagnosticsEventType.EndStage, stage);
    }
}


