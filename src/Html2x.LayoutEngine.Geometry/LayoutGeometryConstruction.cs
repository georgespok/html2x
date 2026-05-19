using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Construction;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.LayoutEngine.Stage.Contracts.Geometry;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry;

/// <summary>
///     Builds published layout geometry output from computed styles.
/// </summary>
/// <remarks>
///     This is the named entry point for the Layout Geometry stage. The implementation may use mutable boxes while
///     resolving layout, but callers receive only <see cref="PublishedLayoutTree" />.
/// </remarks>
internal sealed class LayoutGeometryConstruction(ITextMeasurer textMeasurer) : ILayoutGeometryStage
{
    private readonly BoxTreeConstruction _boxTreeConstruction = new();
    private readonly BlockFormattingMetricsMeasurement _contentMeasurement = new();
    private readonly ITextMeasurer _textMeasurer = textMeasurer ?? throw new ArgumentNullException(nameof(textMeasurer));
    private readonly UnsupportedLayoutModePolicy _unsupportedLayoutModePolicy = new();

    public PublishedLayoutTree Build(LayoutGeometryBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Styles);

        var initialBoxRoot = _boxTreeConstruction.Build(request.Styles);
        _unsupportedLayoutModePolicy.Report(initialBoxRoot, request.DiagnosticsSink);
        return BuildFromInitialBoxes(initialBoxRoot, request.Styles, request.Geometry, request.DiagnosticsSink);
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
