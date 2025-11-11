using AngleSharp;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Microsoft.Extensions.Logging;

namespace Html2x;

public sealed class LayoutBuilderFactory : ILayoutBuilderFactory
{
    public LayoutBuilder Create(ILoggerFactory? loggerFactory = null)
    {
        var angleSharpConfig = Configuration.Default.WithCss();
        return new LayoutBuilder(
            new AngleSharpDomProvider(angleSharpConfig),
            new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults()),
            new BoxTreeBuilder(),
            new FragmentBuilder(),
            loggerFactory?.CreateLogger<LayoutBuilder>());
    }
}
