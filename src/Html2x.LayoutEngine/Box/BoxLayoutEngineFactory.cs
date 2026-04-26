using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Text;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Creates the collaborating layout engines required for one box-tree build.
/// </summary>
internal sealed class BoxLayoutEngineFactory(
    ITextMeasurer? textMeasurer,
    IBlockFormattingContext blockFormattingContext)
{
    private readonly ITextMeasurer? _textMeasurer = textMeasurer;
    private readonly IBlockFormattingContext _blockFormattingContext =
        blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));

    public BlockLayoutEngine Create(
        DiagnosticsSession? diagnosticsSession,
        BoxTreeBuildContext? context)
    {
        var imageResolver = new ImageLayoutResolver(context);
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            _blockFormattingContext,
            InlineNodeMeasurerRegistry.CreateDefault(),
            imageResolver,
            diagnosticsSession);

        return new BlockLayoutEngine(
            inlineEngine,
            new TableLayoutEngine(inlineEngine, imageResolver),
            _blockFormattingContext,
            imageResolver,
            BlockLayoutStrategyRegistry.CreateDefault(),
            diagnosticsSession);
    }
}
