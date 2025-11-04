using System.Drawing;
using Html2x.Core;
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
public class LayoutBuilder(
    IDomProvider domProvider,
    IStyleComputer styleComputer,
    IBoxTreeBuilder boxBuilder,
    IFragmentBuilder fragmentBuilder)
{
    private readonly IBoxTreeBuilder _boxBuilder =
        boxBuilder ?? throw new ArgumentNullException(nameof(boxBuilder));

    private readonly IDomProvider _domProvider =
        domProvider ?? throw new ArgumentNullException(nameof(domProvider));

    private readonly IFragmentBuilder _fragmentBuilder =
        fragmentBuilder ?? throw new ArgumentNullException(nameof(fragmentBuilder));

    private readonly IStyleComputer _styleComputer =
        styleComputer ?? throw new ArgumentNullException(nameof(styleComputer));

    public async Task<HtmlLayout> BuildAsync(string html, Dimensions pageSize)
    {
        var dom = await _domProvider.LoadAsync(html);
        var styleTree = _styleComputer.Compute(dom);
        var boxTree = _boxBuilder.Build(styleTree);
        var fragments = _fragmentBuilder.Build(boxTree);

        var layout = new HtmlLayout();
        var page = new LayoutPage(new SizeF(pageSize.Width, pageSize.Height),
            new Margins(boxTree.Page.MarginTopPt, boxTree.Page.MarginRightPt, boxTree.Page.MarginBottomPt,
                boxTree.Page.MarginLeftPt),
            fragments.Blocks);
        layout.Pages.Add(page);
        return layout;
    }
}