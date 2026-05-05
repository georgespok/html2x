using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Composition;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Formatting;
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
    private readonly StyleTreeBoxProjector _styleTreeBoxProjector;
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
            new StyleTreeBoxProjector(),
            new UnsupportedLayoutModePolicy(),
            textMeasurer,
            blockFormattingContext)
    {
    }

    private LayoutGeometryBuilder(
        StyleTreeBoxProjector styleTreeBoxProjector,
        UnsupportedLayoutModePolicy unsupportedLayoutModePolicy,
        ITextMeasurer? textMeasurer,
        IBlockFormattingContext blockFormattingContext)
    {
        _styleTreeBoxProjector = styleTreeBoxProjector ?? throw new ArgumentNullException(nameof(styleTreeBoxProjector));
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
        var initialBoxRoot = _styleTreeBoxProjector.Build(styles);
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

        var pipeline = GeometryPipelineComposer.Create(
            styles,
            request,
            _textMeasurer,
            _blockFormattingContext,
            diagnosticsSink);
        var layout = pipeline.BlockEngine.LayoutPublished(initialBoxRoot, pipeline.Page);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(layout, diagnosticsSink);
        return layout;
    }
}
