using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Models;

namespace Html2x.LayoutEngine.Box;

/// <summary>
/// Prepares block spacing and dimension inputs for layout consumers.
/// </summary>
internal sealed class BlockMeasurementService
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
        IEnumerable<DisplayNode> children,
        float availableWidth,
        Func<BlockBox, float, float> measureBlockHeight,
        Func<TableBox, float, float> measureTableHeight,
        DiagnosticsSession? diagnosticsSession = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(measureBlockHeight);
        ArgumentNullException.ThrowIfNull(measureTableHeight);

        var syntheticRoot = new BlockBox(DisplayRole.Block)
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
            diagnosticsSession: diagnosticsSession,
            emitDiagnostics: diagnosticsSession is not null,
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
    float ContentBoxWidth);
