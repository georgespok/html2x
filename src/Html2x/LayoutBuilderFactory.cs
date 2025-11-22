using AngleSharp;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;

namespace Html2x;

public sealed class LayoutBuilderFactory : ILayoutBuilderFactory
{
    public LayoutBuilder Create()
    {
        var angleSharpConfig = Configuration.Default.WithCss();

        return new LayoutBuilder(
            new AngleSharpDomProvider(angleSharpConfig),
            new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults(), new CssValueConverter()),
            new BoxTreeBuilder(),
            new FragmentBuilder());
    }
}
