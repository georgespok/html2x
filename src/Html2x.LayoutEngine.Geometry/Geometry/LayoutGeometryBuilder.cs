using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Geometry.Published;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
/// Builds published layout geometry output from computed styles.
/// </summary>
/// <remarks>
/// This is the named Interface for the layout geometry module. The Implementation may use
/// mutable boxes while resolving layout, but callers receive only <see cref="PublishedLayoutTree"/>.
/// </remarks>
internal sealed class LayoutGeometryBuilder
{
    private readonly InitialBoxTreeBuilder _initialBoxTreeBuilder;
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy;
    private readonly ITextMeasurer? _textMeasurer;
    private readonly IBlockFormattingContext _blockFormattingContext;

    public LayoutGeometryBuilder()
        : this(textMeasurer: null, new BlockFormattingContext())
    {
    }

    public LayoutGeometryBuilder(ITextMeasurer textMeasurer)
        : this(textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer)), new BlockFormattingContext())
    {
    }

    internal LayoutGeometryBuilder(ITextMeasurer? textMeasurer, IBlockFormattingContext blockFormattingContext)
        : this(
            new InitialBoxTreeBuilder(),
            new UnsupportedLayoutModePolicy(),
            textMeasurer,
            blockFormattingContext)
    {
    }

    private LayoutGeometryBuilder(
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

    public PublishedLayoutTree Build(
        StyleTree styles,
        DiagnosticsSession? diagnosticsSession = null,
        LayoutGeometryRequest? request = null)
    {
        ArgumentNullException.ThrowIfNull(styles);

        var initialBoxRoot = BuildInitialBoxes(styles, diagnosticsSession);
        return BuildFromInitialBoxes(initialBoxRoot, styles, diagnosticsSession, request);
    }

    private BoxNode BuildInitialBoxes(StyleTree styles, DiagnosticsSession? diagnosticsSession)
    {
        var initialBoxRoot = _initialBoxTreeBuilder.Build(styles);
        _unsupportedLayoutModePolicy.Report(initialBoxRoot, diagnosticsSession);
        return initialBoxRoot;
    }

    private PublishedLayoutTree BuildFromInitialBoxes(
        BoxNode initialBoxRoot,
        StyleTree styles,
        DiagnosticsSession? diagnosticsSession,
        LayoutGeometryRequest? request = null)
    {
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(initialBoxRoot, diagnosticsSession);

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

        var layout = blockEngine.LayoutPublished(initialBoxRoot, page);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(layout, diagnosticsSession);
        return layout;
    }
}
