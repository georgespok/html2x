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
        line.LineHeight.ShouldBe(14.4f, 0.001f);
        line.BaselineY.ShouldBe(line.Rect.Top + 9f, 0.001f);
    }

    [Fact]
    public void InlineRuns_WithDifferentAscent_UseMaxAscentForBaseline()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 500, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("small", new ComputedStyle { FontSizePt = 12 })
                .Inline("BIG", new ComputedStyle { FontSizePt = 20 })
            .Up()
            .BuildTree();

        var context = CreateContext(new SizeBasedTextMeasurer(5f));
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        var expectedAscent = 20f * 0.7f;
        line.BaselineY.ShouldBe(line.Rect.Top + expectedAscent, 0.01f);
    }

    [Fact]
    public void InlineRuns_PropagateDecorations()
    {
        var boxTree = new BlockBoxBuilder()
            .Block(0, 0, 500, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("plain ", new ComputedStyle { FontSizePt = 12 })
                .Inline("underline", new ComputedStyle { FontSizePt = 12, Decorations = TextDecorations.Underline })
                .Inline(" strike", new ComputedStyle { FontSizePt = 12, Decorations = TextDecorations.LineThrough })
            .Up()
            .BuildTree();

        var context = CreateContext(new FakeTextMeasurer(4f, 9f, 3f));
        var state = new FragmentBuildState(boxTree, context);

        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Runs.Count.ShouldBe(3);
        line.Runs[0].Decorations.ShouldBe(TextDecorations.None);
        line.Runs[1].Decorations.ShouldBe(TextDecorations.Underline);
        line.Runs[2].Decorations.ShouldBe(TextDecorations.LineThrough);
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

    private sealed class SizeBasedTextMeasurer(float widthPerChar) : ITextMeasurer
    {
        private readonly float _widthPerChar = widthPerChar;

        public float MeasureWidth(FontKey font, float sizePt, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return text.Length * _widthPerChar;
        }

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
        {
            return (sizePt * 0.7f, sizePt * 0.3f);
        }
    }
}
