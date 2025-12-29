using Html2x.LayoutEngine;

namespace Html2x;

public interface ILayoutBuilderFactory
{
    LayoutBuilder Create();

    LayoutBuilder Create(LayoutServices services);
}
