using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry;

internal static class LayoutGeometryRuntimeFactory
{
    public static LayoutGeometryRuntime Create(
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
            new TableLayoutEngine(inlineEngine, imageResolver),
            blockFormattingContext,
            imageResolver,
            diagnosticsSink);
        var page = new PageBox
        {
            Margin = styles.Page.Margin,
            Size = geometryRequest.PageSize
        };

        return new LayoutGeometryRuntime(blockEngine, page);
    }
}

internal sealed record LayoutGeometryRuntime(BlockLayoutEngine BlockEngine, PageBox Page);
