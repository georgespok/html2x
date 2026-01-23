namespace Html2x.LayoutEngine;

public interface ILayoutBuilderFactory
{
    LayoutBuilder Create(LayoutServices services);
}
