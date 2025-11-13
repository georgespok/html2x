using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public interface IBoxTreeBuilder
{
    BoxTree Build(StyleTree styles);
}