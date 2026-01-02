using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.LayoutEngine.Test;

public class BlockLayoutEngineTests
{
    private readonly Mock<IInlineLayoutEngine> _inlineEngine;
    private readonly Mock<ITableLayoutEngine> _tableLayoutEngine;
    private readonly Mock<IFloatLayoutEngine> _floatLayoutEngine;
    
    private static PageBox DefaultPage() => new()
    {
        Margin = new Spacing(0, 0, 0, 0),
        PageWidthPt = 200,
        PageHeightPt = 400
    };

    public BlockLayoutEngineTests(ITestOutputHelper output) 
    {
        _inlineEngine = new Mock<IInlineLayoutEngine>();
        _tableLayoutEngine = new Mock<ITableLayoutEngine>();
        _floatLayoutEngine = new Mock<IFloatLayoutEngine>();
    }
    
    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        // Arrange
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(24f);

        var root = new BlockBoxBuilder()
            .Block(0, 0, 0, 0, style: new ComputedStyle())
                .WithPadding(top: 10f, bottom: 6f)
            .BuildRoot();

        
        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldHaveSingleItem()
            .Height.ShouldBe(24f + 10f + 6f);
    }


    [Fact]
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        // Arrange
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(10f);

        var root = new BlockBoxBuilder()
            .Inline("root inline")
            .Block(style: new ComputedStyle())
            .Up()
            .BuildRoot();

        
        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldSatisfyAllConditions(
            () => result.Blocks.Count.ShouldBe(2),
            () => result.Blocks[0].IsAnonymous.ShouldBeTrue(),
            () => result.Blocks[0].Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>().TextContent.ShouldBe("root inline"),
            () => result.Blocks[1].IsAnonymous.ShouldBeFalse());
    }

    [Fact]
    public void Layout_AnonymousBlock_DoesNotInheritWidthOrHeightConstraints()
    {
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(10f);

        var root = new BlockBox
        {
            Style = new ComputedStyle
            {
                WidthPt = 60f,
                MinWidthPt = 50f,
                MaxWidthPt = 70f,
                HeightPt = 40f,
                MinHeightPt = 30f,
                MaxHeightPt = 50f
            }
        };

        root.Children.Add(new InlineBox { TextContent = "inline" });
        root.Children.Add(new BlockBox { Style = new ComputedStyle() });

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var anonymous = result.Blocks.FirstOrDefault(b => b.IsAnonymous);
        anonymous.ShouldNotBeNull();
        anonymous!.Style.WidthPt.ShouldBeNull();
        anonymous.Style.MinWidthPt.ShouldBeNull();
        anonymous.Style.MaxWidthPt.ShouldBeNull();
        anonymous.Style.HeightPt.ShouldBeNull();
        anonymous.Style.MinHeightPt.ShouldBeNull();
        anonymous.Style.MaxHeightPt.ShouldBeNull();
    }

    [Fact]
    public void Layout_BlockOnlyChildren_DoesNotCreateAnonymousBlocks()
    {
        // Arrange
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(12f);

        var root = new BlockBoxBuilder()
            .Block(style: new())
            .Up()
            .Block(style: new())
            .BuildRoot();

        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.Count.ShouldBe(2);
        result.Blocks[0].IsAnonymous.ShouldBeFalse();
        result.Blocks[1].IsAnonymous.ShouldBeFalse();
    }
    
    [Theory]
    [InlineData(150f, 150f)] // Smaller than page (200) -> Clamped
    [InlineData(300f, 200f)] // Larger than page (200) -> Page limited
    [InlineData(null, 200f)] // No max width -> Page limited
    public void Layout_RespectsMaxWidth_ClampingToAvailableWidth(float? maxWidthPt, float expectedWidth)
    {
        // Arrange
        var root = new BlockBoxBuilder()
            .Block(style: new ComputedStyle { MaxWidthPt = maxWidthPt })
            .BuildRoot();

        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldHaveSingleItem()
            .Width.ShouldBe(expectedWidth);
    }
    
    private BlockLayoutEngine CreateBlockLayoutEngine() => 
        new(_inlineEngine.Object, _tableLayoutEngine.Object, _floatLayoutEngine.Object);
}
