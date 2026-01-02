using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

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
            Margin = styles.Page.Margin
        };

        return blockEngine.Layout(displayRoot, page);
    }
}
