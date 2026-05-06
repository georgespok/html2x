using Html2x.LayoutEngine.Geometry.Primitives;

namespace Html2x.LayoutEngine.Geometry.Box;

/// <summary>
///     Lays out horizontal rule blocks.
/// </summary>
internal sealed class RuleBlockLayoutRule(
    BlockSizingRules sizingRules,
    LayoutBoxStateWriter stateWriter) : IBlockLayoutRule
{
    private readonly BlockSizingRules
        _sizingRules = sizingRules ?? throw new ArgumentNullException(nameof(sizingRules));

    private readonly LayoutBoxStateWriter _stateWriter =
        stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));

    public bool CanLayout(BlockBox block) => block is RuleBox;

    public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
    {
        var rule = (RuleBox)block;
        var measurement = _sizingRules.Prepare(rule, request.ContentWidth);
        var origin = BlockOriginRules.ResolveOrigin(request, measurement.Margin);

        _stateWriter.ApplyBlockLayout(
            rule,
            measurement,
            UsedGeometryRules.FromBorderBoxWithContentHeight(
                origin.X,
                origin.Y,
                measurement.BorderBoxWidth,
                0f,
                measurement.Padding,
                measurement.Border,
                markerOffset: rule.MarkerOffset));

        return BlockLayoutRuleResult.ForResolvedBlock(rule);
    }
}