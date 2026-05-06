using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Primitives;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test;

public sealed class BlockLayoutRuleSetTests
{
    [Fact]
    public void Layout_UsesFirstRuleThatCanLayoutBlock()
    {
        var block = new BlockBox(BoxRole.Block);
        var customRule = new TestBlockLayoutRule(
            candidate => ReferenceEquals(candidate, block),
            17f);
        var fallbackRule = new TestBlockLayoutRule(
            static _ => true,
            5f);
        var rules = new BlockLayoutRuleSet([customRule, fallbackRule]);

        var result = rules.Layout(block, Request());

        result.Block.ShouldBeSameAs(block);
        block.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(17f);
        customRule.LayoutCount.ShouldBe(1);
        fallbackRule.LayoutCount.ShouldBe(0);
    }

    [Fact]
    public void Layout_LaterRuleCanHandleBlockWithoutChangingFlowLoop()
    {
        var block = new BlockBox(BoxRole.Block);
        var skippedRule = new TestBlockLayoutRule(
            static _ => false,
            17f);
        var addedRule = new TestBlockLayoutRule(
            static _ => true,
            23f);
        var rules = new BlockLayoutRuleSet([skippedRule, addedRule]);

        _ = rules.Layout(block, Request());

        block.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(23f);
        skippedRule.LayoutCount.ShouldBe(0);
        addedRule.LayoutCount.ShouldBe(1);
    }

    private static BlockLayoutRequest Request() =>
        new(
            0f,
            0f,
            100f,
            0f,
            0f,
            0f);

    private sealed class TestBlockLayoutRule(
        Func<BlockBox, bool> canLayout,
        float height) : IBlockLayoutRule
    {
        public int LayoutCount { get; private set; }

        public bool CanLayout(BlockBox block) => canLayout(block);

        public BlockLayoutRuleResult Layout(BlockBox block, BlockLayoutRequest request)
        {
            LayoutCount++;
            block.ApplyLayoutGeometry(UsedGeometryRules.FromBorderBox(
                request.ContentX,
                request.CursorY,
                request.ContentWidth,
                height,
                new(),
                new()));

            return BlockLayoutRuleResult.ForResolvedBlock(block);
        }
    }
}