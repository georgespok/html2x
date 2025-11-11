using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine.Box;

public interface IBoxTreeBuilder
{
    BoxTree Build(StyleTree styles);
}