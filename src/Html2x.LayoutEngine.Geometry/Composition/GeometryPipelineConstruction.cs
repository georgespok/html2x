using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Composition;

internal static class GeometryPipelineConstruction
{
    public static GeometryPipeline Create(
        StyleTree styles,
        LayoutGeometryRequest? request,
        ITextMeasurer? textMeasurer,
        BlockFormattingMetricsMeasurement contentMeasurement,
        IDiagnosticsSink? diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(styles);
        ArgumentNullException.ThrowIfNull(contentMeasurement);

        var geometryRequest = request ?? LayoutGeometryRequest.Default;
        var imageSizingRules = new ImageSizingRules(geometryRequest);
        var inlineFlowLayout = new InlineFlowLayout(
            new DefaultFontMetricsMeasurer(),
            textMeasurer,
            new DefaultLineHeightStrategy(),
            contentMeasurement,
            imageSizingRules,
            diagnosticsSink);
        var blockBoxLayout = new BlockBoxLayout(
            inlineFlowLayout,
            new(inlineFlowLayout, imageSizingRules),
            contentMeasurement,
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
}

internal sealed record GeometryPipeline(BoxTreeLayout BoxTreeLayout, PageBox Page);
