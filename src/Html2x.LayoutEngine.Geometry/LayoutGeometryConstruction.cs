using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
///     Builds published layout geometry output from computed styles.
/// </summary>
/// <remarks>
///     This is the named Interface for the layout geometry module. The Implementation may use
///     mutable boxes while resolving layout, but callers receive only <see cref="PublishedLayoutTree" />.
/// </remarks>
internal sealed class LayoutGeometryConstruction
{
    private readonly BoxTreeConstruction _boxTreeConstruction;
    private readonly BlockFormattingMetricsMeasurement _contentMeasurement;
    private readonly ITextMeasurer? _textMeasurer;
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy;

    public LayoutGeometryConstruction()
        : this(null, new())
    {
    }

    public LayoutGeometryConstruction(ITextMeasurer textMeasurer)
        : this(textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer)), new())
    {
    }

    internal LayoutGeometryConstruction(ITextMeasurer? textMeasurer, BlockFormattingMetricsMeasurement contentMeasurement)
        : this(
            new(),
            new(),
            textMeasurer,
            contentMeasurement)
    {
    }

    private LayoutGeometryConstruction(
        BoxTreeConstruction boxTreeConstruction,
        UnsupportedLayoutModePolicy unsupportedLayoutModePolicy,
        ITextMeasurer? textMeasurer,
        BlockFormattingMetricsMeasurement contentMeasurement)
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

        var pipeline = CreatePipeline(
            styles,
            request,
            diagnosticsSink);
        var layout = pipeline.BoxTreeLayout.Layout(initialBoxRoot, pipeline.Page);
        GeometryLayoutStructureValidator.ValidateInlineBlockStructures(layout, diagnosticsSink);
        return layout;
    }

    private GeometryPipeline CreatePipeline(
        StyleTree styles,
        LayoutGeometryRequest? request,
        IDiagnosticsSink? diagnosticsSink)
    {
        var geometryRequest = request ?? LayoutGeometryRequest.Default;
        var imageSizingRules = new ImageSizingRules(geometryRequest);
        var inlineFlowLayout = new InlineFlowLayout(
            new DefaultFontMetricsMeasurer(),
            _textMeasurer,
            new LineHeightRules(),
            _contentMeasurement,
            imageSizingRules,
            diagnosticsSink);
        var blockBoxLayout = new BlockBoxLayout(
            inlineFlowLayout,
            new(inlineFlowLayout, imageSizingRules),
            _contentMeasurement,
            imageSizingRules,
            diagnosticsSink);
        var boxTreeLayout = new BoxTreeLayout(blockBoxLayout);
        var page = new PageBox
        {
            Margin = styles.Page.Margin,
            Size = geometryRequest.PageSize
        };

        return new(boxTreeLayout, page);
    }

    private sealed record GeometryPipeline(BoxTreeLayout BoxTreeLayout, PageBox Page);
}
