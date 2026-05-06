using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Composition;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
///     Builds published layout geometry output from computed styles.
/// </summary>
/// <remarks>
///     This is the named Interface for the layout geometry module. The Implementation may use
///     mutable boxes while resolving layout, but callers receive only <see cref="PublishedLayoutTree" />.
/// </remarks>
internal sealed class LayoutGeometryBuilder
{
    private readonly BoxTreeConstruction _boxTreeConstruction;
    private readonly BlockContentExtentMeasurement _contentMeasurement;
    private readonly ITextMeasurer? _textMeasurer;
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy;

    public LayoutGeometryBuilder()
        : this(null, new())
    {
    }

    public LayoutGeometryBuilder(ITextMeasurer textMeasurer)
        : this(textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer)), new())
    {
    }

    internal LayoutGeometryBuilder(ITextMeasurer? textMeasurer, BlockContentExtentMeasurement contentMeasurement)
        : this(
            new(),
            new(),
            textMeasurer,
            contentMeasurement)
    {
    }

    private LayoutGeometryBuilder(
        BoxTreeConstruction boxTreeConstruction,
        UnsupportedLayoutModePolicy unsupportedLayoutModePolicy,
        ITextMeasurer? textMeasurer,
        BlockContentExtentMeasurement contentMeasurement)
    {
        _boxTreeConstruction = boxTreeConstruction ?? throw new ArgumentNullException(nameof(boxTreeConstruction));
        _unsupportedLayoutModePolicy = unsupportedLayoutModePolicy
                                       ?? throw new ArgumentNullException(nameof(unsupportedLayoutModePolicy));
        _textMeasurer = textMeasurer;
        _contentMeasurement = contentMeasurement ?? throw new ArgumentNullException(nameof(contentMeasurement));
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
        var initialBoxRoot = _boxTreeConstruction.Build(styles);
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
            _contentMeasurement,
            diagnosticsSink);
        var layout = pipeline.BoxTreeLayout.Layout(initialBoxRoot, pipeline.Page);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(layout, diagnosticsSink);
        return layout;
    }
}