using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

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
        LayoutGeometryRequest? request = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(styles);

        var initialBoxRoot = BuildInitialBoxes(styles, diagnosticsSink);
        return BuildFromInitialBoxes(initialBoxRoot, styles, request, diagnosticsSink);
    }

    private BoxNode BuildInitialBoxes(
        StyleTree styles,
        IDiagnosticsSink? diagnosticsSink)
    {
        var initialBoxRoot = _initialBoxTreeBuilder.Build(styles);
        _unsupportedLayoutModePolicy.Report(initialBoxRoot, diagnosticsSink);
        return initialBoxRoot;
    }

    private PublishedLayoutTree BuildFromInitialBoxes(
        BoxNode initialBoxRoot,
        StyleTree styles,
        LayoutGeometryRequest? request = null,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(initialBoxRoot, diagnosticsSink);

        var runtime = LayoutGeometryRuntimeFactory.Create(
            styles,
            request,
            _textMeasurer,
            _blockFormattingContext,
            diagnosticsSink);
        var layout = runtime.BlockEngine.LayoutPublished(initialBoxRoot, runtime.Page);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(layout, diagnosticsSink);
        return layout;
    }
}
