namespace Html2x.LayoutEngine.Geometry.Box;

internal interface IBlockLayoutRule
{
    bool CanLayout(BlockBox block);

    BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request);
}
