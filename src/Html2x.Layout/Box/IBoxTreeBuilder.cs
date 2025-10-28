using Html2x.Layout.Style;

namespace Html2x.Layout.Box;

public interface IBoxTreeBuilder
{
    BoxTree Build(StyleTree styles);
}