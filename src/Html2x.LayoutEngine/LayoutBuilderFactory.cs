using AngleSharp;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Style;

namespace Html2x.LayoutEngine;

/// <summary>
/// Creates the default layout pipeline with shared formatting-context ownership.
/// </summary>
public sealed class LayoutBuilderFactory : ILayoutBuilderFactory
{
    public LayoutBuilder Create(LayoutServices services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var angleSharpConfig = Configuration.Default.WithCss();
        var blockFormattingContext = CreateBlockFormattingContext();
        
        return new LayoutBuilder(
            new AngleSharpDomProvider(angleSharpConfig),
            new CssStyleComputer(new StyleTraversal(), new CssValueConverter()),
            new BoxTreeBuilder(services.TextMeasurer, blockFormattingContext),
            new FragmentBuilder(),
            services.ImageProvider,
            services.TextMeasurer,
            services.FontSource,
            blockFormattingContext);
    }

    private static IBlockFormattingContext CreateBlockFormattingContext()
    {
        return new BlockFormattingContext();
    }
}
