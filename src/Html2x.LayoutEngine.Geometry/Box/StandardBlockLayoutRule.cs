using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
/// Lays out normal block boxes that do not need specialized replaced, rule, or table behavior.
/// </summary>
internal sealed class StandardBlockLayoutRule(
    BoxSizingRules sizingRules,
    BlockFlowLayoutExecutor blockFlow,
    LayoutBoxStateWriter stateWriter) : IBlockLayoutRule
{
    private readonly BoxSizingRules _sizingRules = sizingRules ?? throw new ArgumentNullException(nameof(sizingRules));
    private readonly BlockFlowLayoutExecutor _blockFlow = blockFlow ?? throw new ArgumentNullException(nameof(blockFlow));
    private readonly LayoutBoxStateWriter _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public bool CanLayout(BlockBox block)
    {
        return block is not TableBox and not ImageBox and not RuleBox;
    }

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var flowLayout = ApplyLayout(block, request);
        return BlockLayoutRuleResult.ForFlow(block, flowLayout);
    }

    private BlockFlowLayoutResult ApplyLayout(BlockBox block, BlockLayoutRequest request)
    {
        var measurement = _sizingRules.Prepare(block, request.ContentWidth);
        var origin = BlockOriginResolver.ResolveOrigin(request, measurement.Margin);
        var contentArea = UsedGeometryCalculator.ResolveContentFlowArea(
            origin.X,
            origin.Y,
            measurement.BorderBoxWidth,
            0f,
            measurement.Padding,
            measurement.Border,
            block.MarkerOffset);

        _stateWriter.ApplyTextAlignment(block);

        var flowLayout = _blockFlow.Layout(new BlockFlowLayoutRequest(
            block,
            contentArea.X,
            contentArea.Y,
            contentArea.Width,
            contentArea.Y));
        var contentHeight = _sizingRules.ResolveContentHeight(
            block,
            flowLayout.ContentHeight);

        _stateWriter.ApplyBlockLayout(
            block,
            measurement,
            UsedGeometryCalculator.FromBorderBoxWithContentHeight(
                origin.X,
                origin.Y,
                UsedGeometryCalculator.RequireNonNegativeFinite(measurement.BorderBoxWidth),
                UsedGeometryCalculator.RequireNonNegativeFinite(contentHeight),
                measurement.Padding,
                measurement.Border,
                markerOffset: block.MarkerOffset));

        return flowLayout;
    }
}
