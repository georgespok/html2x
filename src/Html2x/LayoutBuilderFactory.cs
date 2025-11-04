using AngleSharp;
using Html2x.Layout;
using Html2x.Layout.Box;
using Html2x.Layout.Dom;
using Html2x.Layout.Fragment;
using Html2x.Layout.Style;

namespace Html2x;

public sealed class LayoutBuilderFactory : ILayoutBuilderFactory
{
    public LayoutBuilder Create()
    {
        var angleSharpConfig = Configuration.Default.WithCss();
        return new LayoutBuilder(
            new AngleSharpDomProvider(angleSharpConfig),
            new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults()),
            new BoxTreeBuilder(),
            new FragmentBuilder());
    }
}