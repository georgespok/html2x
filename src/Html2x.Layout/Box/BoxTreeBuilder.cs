using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

public class BoxTreeBuilder : IBoxTreeBuilder
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
            MarginTopPt = styles.Page.MarginTopPt,
            MarginRightPt = styles.Page.MarginRightPt,
            MarginBottomPt = styles.Page.MarginBottomPt,
            MarginLeftPt = styles.Page.MarginLeftPt
        };

        return blockEngine.Layout(displayRoot, page);
    }
}