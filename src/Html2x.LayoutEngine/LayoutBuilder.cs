using System.Drawing;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Spacing = Html2x.Abstractions.Layout.Styles.Spacing;

namespace Html2x.LayoutEngine;

/// <summary>
///     High-level orchestrator that enforces the layered pipeline:
///     DOM/CSSOM -> Style Tree -> Box Tree -> Fragment Tree -> HtmlLayout
/// </summary>
public class LayoutBuilder(
    IDomProvider domProvider,
    IStyleComputer styleComputer,
    IBoxTreeBuilder boxBuilder,
    IFragmentBuilder fragmentBuilder)
{
    private const string LayoutSnapshotCategory = "snapshot/layout";

    private readonly IBoxTreeBuilder _boxBuilder = boxBuilder ?? throw new ArgumentNullException(nameof(boxBuilder));
    private readonly IDomProvider _domProvider = domProvider ?? throw new ArgumentNullException(nameof(domProvider));
    private readonly IFragmentBuilder _fragmentBuilder = fragmentBuilder ?? throw new ArgumentNullException(nameof(fragmentBuilder));
    private readonly IStyleComputer _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
    
    public async Task<HtmlLayout> BuildAsync(string html, 
        LayoutOptions options, DiagnosticsSession? diagnosticsSession = null)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var dom = await _domProvider.LoadAsync(html);

        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.EndStage,
            Name = "stage/dom",
            Description = "DOM loaded",

        });

        var styleTree = RunStage("stage/style", () => _styleComputer.Compute(dom));

        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.EndStage,
            Name = "stage/style"
        });
        
        var boxTree = RunStage("stage/layout", () => _boxBuilder.Build(styleTree));
        
        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.EndStage,
            Name = "stage/layout"
        });

        var fragments = RunStage("stage/inline-measurement", () => _fragmentBuilder.Build(boxTree));

        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.EndStage,
            Name = "stage/inline-measurement"
        });
        
        RunStage("stage/fragmentation", () =>
        {
            // Fragmentation stage currently aligns with fragment building; placeholder for future expansion.
        });

        var layout = RunStage("stage/pagination", () =>
        {
            var newLayout = new HtmlLayout();
            var page = new LayoutPage(new SizeF(options.PageSize.Width, options.PageSize.Height),
                new Spacing(boxTree.Page.MarginTopPt, boxTree.Page.MarginRightPt, boxTree.Page.MarginBottomPt,
                    boxTree.Page.MarginLeftPt),
                fragments.Blocks);
            newLayout.Pages.Add(page);
            return newLayout;
        });

        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.EndStage,
            Name = "stage/pagination"
        });

        return layout;
    }

    private T RunStage<T>(string stage, Func<T> action)
    {
        return action();
    }

    private void RunStage(string stage, Action action)
    {
        action();
    }
}


