using System.Drawing;
using Html2x.Core;
using Html2x.Core.Layout;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Spacing = Html2x.Core.Layout.Spacing;

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
    private readonly ILogger<LayoutBuilder> _logger;

    public LayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        ILogger<LayoutBuilder>? logger = null)
    {
        _domProvider = domProvider ?? throw new ArgumentNullException(nameof(domProvider));
        _styleComputer = styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));
        _boxBuilder = boxBuilder ?? throw new ArgumentNullException(nameof(boxBuilder));
        _fragmentBuilder = fragmentBuilder ?? throw new ArgumentNullException(nameof(fragmentBuilder));
        _logger = logger ?? NullLogger<LayoutBuilder>.Instance;
    }

    public async Task<HtmlLayout> BuildAsync(string html, PageSize pageSize)
    {
        LayoutLog.BuildStart(_logger, html?.Length ?? 0, pageSize);

        var dom = await _domProvider.LoadAsync(html);
        LayoutLog.StageComplete(_logger, "DomLoaded");

        var styleTree = _styleComputer.Compute(dom);
        LayoutLog.StageComplete(_logger, "StylesComputed");

        var boxTree = _boxBuilder.Build(styleTree);
        LayoutLog.StageComplete(_logger, "BoxTreeBuilt");

        var fragments = _fragmentBuilder.Build(boxTree);
        LayoutLog.StageComplete(_logger, "FragmentsBuilt");

        var layout = new HtmlLayout();
        var page = new LayoutPage(new SizeF(pageSize.Width, pageSize.Height),
            new Spacing(boxTree.Page.MarginTopPt, boxTree.Page.MarginRightPt, boxTree.Page.MarginBottomPt,
                boxTree.Page.MarginLeftPt),
            fragments.Blocks);
        layout.Pages.Add(page);

        LayoutLog.BuildComplete(_logger, fragments.Blocks.Count);
        return layout;
    }
}
