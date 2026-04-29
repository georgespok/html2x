using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Builds the legacy mutable box tree used by layout implementation tests and diagnostics harnesses.
/// </summary>
/// <remarks>
/// Production layout should use LayoutGeometryBuilder so callers depend on PublishedLayoutTree
/// instead of mutable box internals.
/// </remarks>
internal sealed class BoxTreeBuilder
{
    private readonly InitialBoxTreeBuilder _initialBoxTreeBuilder;
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy;
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
        : this(
            new InitialBoxTreeBuilder(),
            new UnsupportedLayoutModePolicy(),
            textMeasurer,
            blockFormattingContext)
    {
    }

    private BoxTreeBuilder(
        InitialBoxTreeBuilder initialBoxTreeBuilder,
        UnsupportedLayoutModePolicy unsupportedLayoutModePolicy,
        ITextMeasurer? textMeasurer,
        IBlockFormattingContext blockFormattingContext)
    {
        _initialBoxTreeBuilder = initialBoxTreeBuilder ?? throw new ArgumentNullException(nameof(initialBoxTreeBuilder));
        _unsupportedLayoutModePolicy = unsupportedLayoutModePolicy
                                       ?? throw new ArgumentNullException(nameof(unsupportedLayoutModePolicy));
        _textMeasurer = textMeasurer;
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
    }

    public BoxTree Build(StyleTree styles, DiagnosticsSession? diagnosticsSession = null, LayoutGeometryRequest? request = null)
    {
        var initialBoxRoot = BuildInitial(styles, diagnosticsSession);
        var boxTree = ApplyLayoutGeometry(initialBoxRoot, styles, diagnosticsSession, request);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(boxTree, diagnosticsSession);
        return boxTree;
    }

    private BoxNode BuildInitial(StyleTree styles, DiagnosticsSession? diagnosticsSession)
    {
        ArgumentNullException.ThrowIfNull(styles);

        var initialBoxRoot = _initialBoxTreeBuilder.Build(styles);
        _unsupportedLayoutModePolicy.Report(initialBoxRoot, diagnosticsSession);
        return initialBoxRoot;
    }

    private BoxTree ApplyLayoutGeometry(
        BoxNode initialBoxRoot,
        StyleTree styles,
        DiagnosticsSession? diagnosticsSession,
        LayoutGeometryRequest? request)
    {
        ArgumentNullException.ThrowIfNull(initialBoxRoot);
        ArgumentNullException.ThrowIfNull(styles);

        var geometryRequest = request ?? LayoutGeometryRequest.Default;
        var imageResolver = new ImageLayoutResolver(geometryRequest);
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            _blockFormattingContext,
            imageResolver,
            diagnosticsSession);
        var blockEngine = new BlockLayoutEngine(
            inlineEngine,
            new TableLayoutEngine(inlineEngine, imageResolver),
            _blockFormattingContext,
            imageResolver,
            diagnosticsSession);

        var page = new PageBox
        {
            Margin = styles.Page.Margin,
            Size = geometryRequest.PageSize
        };

        return blockEngine.Layout(initialBoxRoot, page);
    }
}
