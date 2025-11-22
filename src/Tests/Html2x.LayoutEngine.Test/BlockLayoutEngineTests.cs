using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class BlockLayoutEngineTests
{
    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        // Arrange
        var inlineEngine = new StubInlineLayoutEngine(24f);
        var engine = new BlockLayoutEngine(inlineEngine, new StubTableLayoutEngine(), new StubFloatLayoutEngine());

        var block = new BlockBox
        {
            Style = new ComputedStyle
            {
                PaddingTopPt = 10f,
                PaddingBottomPt = 6f
            }
        };

        var root = new BlockBox();
        root.Children.Add(block);

        var page = new PageBox
        {
            PageWidthPt = 200,
            PageHeightPt = 400,
            MarginLeftPt = 0,
            MarginTopPt = 0,
            MarginRightPt = 0,
            MarginBottomPt = 0
        };

        // Act
        var result = engine.Layout(root, page);

        // Assert
        result.Blocks.ShouldHaveSingleItem()
            .Height.ShouldBe(24f + 10f + 6f);
    }

    private sealed class StubInlineLayoutEngine(float height) : IInlineLayoutEngine
    {
        public float MeasureHeight(DisplayNode block, float availableWidth) => height;
    }

    [Fact]
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        // Arrange
        var inlineEngine = new StubInlineLayoutEngine(10f);
        var engine = new BlockLayoutEngine(inlineEngine, new StubTableLayoutEngine(), new StubFloatLayoutEngine());

        var inline = new InlineBox
        {
            TextContent = "root inline",
            Style = new ComputedStyle()
        };

        var explicitBlock = new BlockBox
        {
            Style = new ComputedStyle()
        };

        var root = new BlockBox();
        root.Children.Add(inline);
        root.Children.Add(explicitBlock);

        var page = new PageBox
        {
            PageWidthPt = 200,
            PageHeightPt = 400
        };

        // Act
        var result = engine.Layout(root, page);

        // Assert
        result.Blocks.Count.ShouldBe(2);

        var anon = result.Blocks[0];
        anon.IsAnonymous.ShouldBeTrue();
        anon.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>().TextContent.ShouldBe("root inline");

        var second = result.Blocks[1];
        second.IsAnonymous.ShouldBeFalse();
        second.ShouldBeSameAs(explicitBlock);
    }

    [Fact]
    public void Layout_BlockOnlyChildren_DoesNotCreateAnonymousBlocks()
    {
        // Arrange
        var inlineEngine = new StubInlineLayoutEngine(12f);
        var engine = new BlockLayoutEngine(inlineEngine, new StubTableLayoutEngine(), new StubFloatLayoutEngine());

        var first = new BlockBox { Style = new ComputedStyle() };
        var second = new BlockBox { Style = new ComputedStyle() };

        var root = new BlockBox();
        root.Children.Add(first);
        root.Children.Add(second);

        var page = new PageBox
        {
            PageWidthPt = 200,
            PageHeightPt = 400
        };

        // Act
        var result = engine.Layout(root, page);

        // Assert
        result.Blocks.Count.ShouldBe(2);
        result.Blocks[0].IsAnonymous.ShouldBeFalse();
        result.Blocks[1].IsAnonymous.ShouldBeFalse();
    }

    private sealed class StubTableLayoutEngine : ITableLayoutEngine
    {
        public float MeasureHeight(TableBox table, float availableWidth) => 0f;
    }

    private sealed class StubFloatLayoutEngine : IFloatLayoutEngine
    {
        public void PlaceFloats(DisplayNode block, float x, float y, float width)
        {
        }
    }
}
