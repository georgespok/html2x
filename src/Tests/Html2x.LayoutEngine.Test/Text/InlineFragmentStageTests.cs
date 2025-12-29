using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Fragment.Stages;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public class InlineFragmentStageTests
{
    [Fact]
    public void InlineSiblingsWithSameBaseline_AreMergedIntoSingleLine()
    {
        // Arrange: build a single block with sibling inline nodes via the builder helpers
        var boxTree = new BlockBoxBuilder()
            .Block(10, 20, 200, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("This is ", new ComputedStyle { FontSizePt = 12 })
                .Inline("bold", new ComputedStyle { FontSizePt = 12, Bold = true })
                .Inline(" and ", new ComputedStyle { FontSizePt = 12 })
                .Inline("italic", new ComputedStyle { FontSizePt = 12, Italic = true })
            .Inline(" text.", new ComputedStyle { FontSizePt = 12 })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(0f, 9f, 3f));
        var state = new FragmentBuildState(boxTree, context);
        
        // Act
        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        
        // Assert
        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Runs.Count.ShouldBe(5);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("This is bold and italic text.");

        foreach (var run in line.Runs)
        {
            run.Origin.Y.ShouldBe(line.BaselineY, 0.01);
        }

        for (var i = 1; i < line.Runs.Count; i++)
        {
            line.Runs[i].Origin.X.ShouldBeGreaterThanOrEqualTo(line.Runs[i - 1].Origin.X);
        }
    }

    [Fact]
    public void InlineText_UsesMeasuredWidthAndMetrics()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 200, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("Measure me", new ComputedStyle { FontSizePt = 12 })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Rect.Width.ShouldBe(40f);
        line.LineHeight.ShouldBe(12f);
        line.BaselineY.ShouldBe(line.Rect.Top + 9f, 0.001f);
    }

    private static FragmentBuildContext CreateContext(ITextMeasurer textMeasurer)
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentBuildContext(
            new NoopImageProvider(),
            Directory.GetCurrentDirectory(),
            (long)(10 * 1024 * 1024),
            textMeasurer,
            fontSource.Object);
    }
}
