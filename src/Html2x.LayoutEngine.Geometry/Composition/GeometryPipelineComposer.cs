using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Composition;

internal static class GeometryPipelineComposer
{
    public static GeometryPipeline Create(
        StyleTree styles,
        LayoutGeometryRequest? request,
        ITextMeasurer? textMeasurer,
        BlockContentExtentMeasurement contentMeasurement,
        IDiagnosticsSink? diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(styles);
        ArgumentNullException.ThrowIfNull(contentMeasurement);

        var geometryRequest = request ?? LayoutGeometryRequest.Default;
        var imageResolver = new ImageSizingRules(geometryRequest);
        var inlineFlowLayout = new InlineFlowLayout(
            new FontMetricsProvider(),
            textMeasurer,
            new DefaultLineHeightStrategy(),
            contentMeasurement,
            imageResolver,
            diagnosticsSink);
        var blockBoxLayout = new BlockBoxLayout(
            inlineFlowLayout,
            new(inlineFlowLayout, imageResolver),
            contentMeasurement,
            imageResolver,
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
