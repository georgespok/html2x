using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

public class BoxTreeBuilder : IBoxTreeBuilder
{
    private readonly ITextMeasurer? _textMeasurer;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public BoxTreeBuilder()
        : this(textMeasurer: null, new BlockFormattingContext())
    {
    }

    public BoxTreeBuilder(ITextMeasurer textMeasurer)
        : this(textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer)), new BlockFormattingContext())
    {
    }

    internal BoxTreeBuilder(ITextMeasurer? textMeasurer, IBlockFormattingContext blockFormattingContext)
    {
        _textMeasurer = textMeasurer;
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }

    public BoxTree Build(StyleTree styles, DiagnosticsSession? diagnosticsSession = null, BoxTreeBuildContext? context = null)
    {
        var displayTreeBuilder = new DisplayTreeBuilder();
        var displayRoot = displayTreeBuilder.Build(styles);
        var imageResolver = new ImageLayoutResolver(context);
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            _blockFormattingContext,
            Text.InlineNodeMeasurerRegistry.CreateDefault(),
            imageResolver);

        var blockEngine = new BlockLayoutEngine(
            inlineEngine,
            new TableLayoutEngine(inlineEngine, imageResolver),
            new FloatLayoutEngine(),
            _blockFormattingContext,
            imageResolver,
            BlockLayoutStrategyRegistry.CreateDefault(),
            diagnosticsSession);

        var page = new PageBox
        {
            Margin = styles.Page.Margin
        };

        return blockEngine.Layout(displayRoot, page);
    }
}
