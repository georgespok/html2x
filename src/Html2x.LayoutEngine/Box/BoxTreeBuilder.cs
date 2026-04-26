using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Converts computed styles into display nodes, then applies block layout geometry using the active layout context.
/// </summary>
public class BoxTreeBuilder : IBoxTreeBuilder
{
    private readonly DisplayTreeBuilder _displayTreeBuilder;
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy;
    private readonly BoxLayoutEngineFactory _layoutEngineFactory;

    public BoxTreeBuilder()
        : this(textMeasurer: null, new BlockFormattingContext())
    {
    }

    public BoxTreeBuilder(ITextMeasurer textMeasurer)
        : this(textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer)), new BlockFormattingContext())
    {
    }

    internal BoxTreeBuilder(ITextMeasurer? textMeasurer, IBlockFormattingContext blockFormattingContext)
        : this(
            new DisplayTreeBuilder(),
            new UnsupportedLayoutModePolicy(),
            new BoxLayoutEngineFactory(textMeasurer, blockFormattingContext))
    {
    }

    private BoxTreeBuilder(
        DisplayTreeBuilder displayTreeBuilder,
        UnsupportedLayoutModePolicy unsupportedLayoutModePolicy,
        BoxLayoutEngineFactory layoutEngineFactory)
    {
        _displayTreeBuilder = displayTreeBuilder ?? throw new ArgumentNullException(nameof(displayTreeBuilder));
        _unsupportedLayoutModePolicy = unsupportedLayoutModePolicy
                                       ?? throw new ArgumentNullException(nameof(unsupportedLayoutModePolicy));
        _layoutEngineFactory = layoutEngineFactory ?? throw new ArgumentNullException(nameof(layoutEngineFactory));
    }

    public DisplayNode BuildDisplayTree(StyleTree styles, DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(styles);

        var displayRoot = _displayTreeBuilder.Build(styles);
        _unsupportedLayoutModePolicy.Report(displayRoot, diagnosticsSession);
        return displayRoot;
    }

    public BoxTree BuildLayoutGeometry(
        DisplayNode displayRoot,
        StyleTree styles,
        DiagnosticsSession? diagnosticsSession = null,
        BoxTreeBuildContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(displayRoot);
        ArgumentNullException.ThrowIfNull(styles);

        var blockEngine = _layoutEngineFactory.Create(diagnosticsSession, context);

        var page = new PageBox
        {
            Margin = styles.Page.Margin
        };

        return blockEngine.Layout(displayRoot, page);
    }

    public BoxTree Build(StyleTree styles, DiagnosticsSession? diagnosticsSession = null, BoxTreeBuildContext? context = null)
    {
        var displayRoot = BuildDisplayTree(styles, diagnosticsSession);
        return BuildLayoutGeometry(displayRoot, styles, diagnosticsSession, context);
    }
}
