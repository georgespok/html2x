using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Style;

namespace Html2x.LayoutEngine.Box;


/// <summary>
/// Prepares block spacing and dimension inputs for layout consumers.
/// </summary>
internal sealed class BlockMeasurementService : IBlockGeometryMeasurer
{
    private readonly IBlockFormattingContext _blockFormattingContext;
    private readonly BlockFlowMeasurementExecutor _flowMeasurement;

    public BlockMeasurementService()
        : this(new BlockFormattingContext())
    {
    }

    internal BlockMeasurementService(IBlockFormattingContext blockFormattingContext)
    {
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
        _flowMeasurement = new BlockFlowMeasurementExecutor(_blockFormattingContext);
    }

    public BlockMeasurementBasis Prepare(BlockBox box, float availableWidth)
    {
        return _blockFormattingContext.Measure(box, availableWidth);
    }

    public BlockMeasurementBasis PrepareAtomic(BlockBox box, float availableWidth)
    {
        ArgumentNullException.ThrowIfNull(box);

        var style = box.Style;
        var margin = style.Margin.Safe();
        var padding = style.Padding.Safe();
        var border = Spacing.FromBorderEdges(style.Borders).Safe();
        var borderBoxWidth = BoxDimensionResolver.ResolveAtomicBorderBoxWidth(
            style,
            availableWidth,
            padding,
            border);
        var contentFlowWidth = BoxDimensionResolver.ResolveContentFlowWidth(
            borderBoxWidth,
            padding,
            border,
            box.MarkerOffset);

        return new BlockMeasurementBasis(
            margin,
            padding,
            border,
            borderBoxWidth,
            contentFlowWidth);
    }

    public float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f)
    {
        ArgumentNullException.ThrowIfNull(box);
        return BoxDimensionResolver.ResolveContentBoxHeight(
            box.Style,
            resolvedContentHeight,
            minimumContentHeight);
    }

    public float MeasureStackedChildBlocks(
        IEnumerable<BoxNode> children,
        float availableWidth,
        Func<BlockBox, float, float> measureBlockHeight,
        Func<TableBox, float, float> measureTableHeight,
        IDiagnosticsSink? diagnosticsSink = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(measureBlockHeight);
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        return _flowMeasurement.TryMeasureStackedChildren(
            children,
            availableWidth,
            out var totalHeight,
            measureBlockHeight,
            measureTableHeight,
            diagnosticsSink,
            formattingContext,
            nameof(BlockMeasurementService))
            ? totalHeight
            : 0f;
    }
}
