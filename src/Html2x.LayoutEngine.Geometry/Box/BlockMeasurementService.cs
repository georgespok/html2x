using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Prepares block spacing and dimension inputs for layout consumers.
/// </summary>
internal interface IBlockGeometryMeasurer
{
    BlockMeasurementBasis Prepare(BlockBox box, float availableWidth);

    BlockMeasurementBasis PrepareAtomic(BlockBox box, float availableWidth);

    float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f);
}

/// <summary>
/// Prepares block spacing and dimension inputs for layout consumers.
/// </summary>
internal sealed class BlockMeasurementService : IBlockGeometryMeasurer
{
    private readonly IBlockFormattingContext _blockFormattingContext;

    public BlockMeasurementService()
        : this(new BlockFormattingContext())
    {
    }

    internal BlockMeasurementService(IBlockFormattingContext blockFormattingContext)
    {
        _blockFormattingContext = blockFormattingContext ?? throw new ArgumentNullException(nameof(blockFormattingContext));
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

        var syntheticRoot = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };

        foreach (var child in children)
        {
            if (child is BlockBox block)
            {
                syntheticRoot.Children.Add(block);
            }
        }

        var request = new BlockFormattingRequest(
            formattingContext,
            syntheticRoot,
            availableWidth,
            consumerName: nameof(BlockMeasurementService),
            diagnosticsSink: diagnosticsSink,
            emitDiagnostics: diagnosticsSink is not null,
            blockHeightMeasurer: measureBlockHeight,
            tableHeightMeasurer: measureTableHeight);

        return _blockFormattingContext.Format(request).TotalHeight;
    }
}

/// <summary>
/// Carries resolved block spacing and width values for one layout pass.
/// </summary>
internal readonly record struct BlockMeasurementBasis(
    Spacing Margin,
    Spacing Padding,
    Spacing Border,
    float BorderBoxWidth,
    float ContentFlowWidth);
