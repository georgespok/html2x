using System.Drawing;
using AngleSharp;
using Html2x.Core.Layout;
using Html2x.Layout.Box;
using Html2x.Layout.Dom;
using Html2x.Layout.Fragment;
using Html2x.Layout.Style;

namespace Html2x.Layout;

/// <summary>
///     High-level orchestrator that enforces the layered pipeline:
///     DOM/CSSOM → Style Tree → Box Tree → Fragment Tree → HtmlLayout
/// </summary>
public class LayoutBuilder
{
    private readonly IBoxTreeBuilder _boxBuilder;
    private readonly IDomProvider _domProvider;
    private readonly IFragmentBuilder _fragmentBuilder;
    private readonly IStyleComputer _styleComputer;

    public LayoutBuilder()
    {
        var cfg = Configuration.Default.WithCss();

        _domProvider = new AngleSharpDomProvider(cfg);
        _styleComputer = new CssStyleComputer();
        _boxBuilder = new BoxTreeBuilder();
        _fragmentBuilder = new FragmentBuilder();
    }

    public async Task<HtmlLayout> BuildAsync(string html)
    {
        var dom = await _domProvider.LoadAsync(html);
        var styleTree = _styleComputer.Compute(dom);
        var boxTree = _boxBuilder.Build(styleTree);
        var fragments = _fragmentBuilder.Build(boxTree);

        // Wrap fragments into HtmlLayout (single page MVP)
        var layout = new HtmlLayout();
        var page = new LayoutPage(new SizeF(595, 842),
            new Margins(boxTree.Page.MarginTopPt, boxTree.Page.MarginRightPt, boxTree.Page.MarginBottomPt,
                boxTree.Page.MarginLeftPt),
            fragments.Blocks);
        layout.Pages.Add(page);
        return layout;
    }
}