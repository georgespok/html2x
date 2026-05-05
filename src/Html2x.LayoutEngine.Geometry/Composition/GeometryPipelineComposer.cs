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
        IBlockFormattingContext blockFormattingContext,
        IDiagnosticsSink? diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(styles);
        ArgumentNullException.ThrowIfNull(blockFormattingContext);

        var geometryRequest = request ?? LayoutGeometryRequest.Default;
        var imageResolver = new ImageLayoutResolver(geometryRequest);
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            textMeasurer,
            new DefaultLineHeightStrategy(),
            blockFormattingContext,
            imageResolver,
            diagnosticsSink);
        var blockEngine = new BlockLayoutEngine(
            inlineEngine,
            new TableGridLayout(inlineEngine, imageResolver),
            blockFormattingContext,
            imageResolver,
            diagnosticsSink);
        var page = new PageBox
        {
            Margin = styles.Page.Margin,
            Size = geometryRequest.PageSize
        };

        return new GeometryPipeline(blockEngine, page);
    }
}

internal sealed record GeometryPipeline(BlockLayoutEngine BlockEngine, PageBox Page);
