using Html2x.Layout.Box;
using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test;

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
