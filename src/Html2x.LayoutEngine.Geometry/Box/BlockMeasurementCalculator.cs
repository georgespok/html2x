using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Formatting;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Geometry.Box;


/// <summary>
/// Compatibility adapter for callers that have not moved to <see cref="BoxSizingRules"/>.
/// </summary>
internal sealed class BlockMeasurementCalculator
{
    private readonly BoxSizingRules _sizingRules;

    public BlockMeasurementCalculator()
        : this(new BlockFormattingContext())
    {
    }

    internal BlockMeasurementCalculator(IBlockFormattingContext blockFormattingContext)
    {
        _sizingRules = new BoxSizingRules(blockFormattingContext);
    }

    public BlockMeasurementBasis Prepare(BlockBox box, float availableWidth)
    {
        return _sizingRules.Prepare(box, availableWidth);
    }

    public BlockMeasurementBasis PrepareAtomic(BlockBox box, float availableWidth)
    {
        return _sizingRules.PrepareAtomic(box, availableWidth);
    }

    public float ResolveContentHeight(
        BlockBox box,
        float resolvedContentHeight,
        float minimumContentHeight = 0f)
    {
        return _sizingRules.ResolveContentHeight(
            box,
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
        return _sizingRules.MeasureStackedChildBlocks(
            children,
            availableWidth,
            measureBlockHeight,
            measureTableHeight,
            diagnosticsSink,
            formattingContext);
    }
}
