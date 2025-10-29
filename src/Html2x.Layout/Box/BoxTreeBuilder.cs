using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

internal class BoxTreeBuilder : IBoxTreeBuilder
{
    public BoxTree Build(StyleTree styles)
    {
        var displayTreeBuilder = new DisplayTreeBuilder();
        var displayRoot = displayTreeBuilder.Build(styles);

        var blockEngine = new BlockLayoutEngine(
            new InlineLayoutEngine(),
            new TableLayoutEngine(),
            new FloatLayoutEngine());

        var page = new PageBox
        {
            MarginTopPt = 24,
            MarginRightPt = 24,
            MarginBottomPt = 24,
            MarginLeftPt = 24
        };

        return blockEngine.Layout(displayRoot, page);
    }
}