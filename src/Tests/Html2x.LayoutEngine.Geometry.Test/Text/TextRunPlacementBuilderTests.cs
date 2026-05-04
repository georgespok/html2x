using Html2x.RenderModel;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Text;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public sealed class TextRunPlacementBuilderTests
{
    [Fact]
    public void Build_TextRuns_GroupsSequentialRunsIntoOneTextItem()
    {
        var source = new InlineBox(BoxRole.Inline);
        var resolvedFont = new ResolvedFont("Arial", FontWeight.W400, FontStyle.Normal, "test://arial");
        var line = new TextLayoutLine(
            [
                CreateRun(source, "A", width: 5f, leftSpacing: 1f, rightSpacing: 2f, resolvedFont),
                CreateRun(source, "B", width: 6f, leftSpacing: 3f, rightSpacing: 4f, resolvedFont)
            ],
            LineWidth: 21f,
            LineHeight: 12f);
        var builder = new TextRunPlacementBuilder(
            new InlineObjectPlacementBuilder((_, _, _, _, _, _) => throw new InvalidOperationException("Unexpected inline object.")),
            new InlineLineBoundsCalculator());

        var items = builder.Build(
            line,
            new InlineLinePlacement(
                ContentLeft: 10f,
                TopY: 20f,
                LineHeight: 12f,
                BaselineY: 29f,
                StartX: 10f),
            InlineJustificationPlanner.CreateSequentialTextPlacements);

        var item = items.ShouldHaveSingleItem().ShouldBeOfType<InlineTextItemLayout>();
        item.Runs.Count.ShouldBe(2);
        item.Runs[0].Text.ShouldBe("A");
        item.Runs[0].Origin.X.ShouldBe(11f);
        item.Runs[0].Origin.Y.ShouldBe(29f);
        item.Runs[0].ResolvedFont.ShouldBe(resolvedFont);
        item.Runs[1].Text.ShouldBe("B");
        item.Runs[1].Origin.X.ShouldBe(21f);
        item.Runs[1].ResolvedFont.ShouldBe(resolvedFont);
        item.Sources.ShouldHaveSingleItem().ShouldBeSameAs(source);
    }

    private static TextLayoutRun CreateRun(
        InlineBox source,
        string text,
        float width,
        float leftSpacing,
        float rightSpacing,
        ResolvedFont? resolvedFont = null)
    {
        return new TextLayoutRun(
            source,
            text,
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            12f,
            width,
            leftSpacing,
            rightSpacing,
            9f,
            3f,
            TextDecorations.None,
            ColorRgba.Black,
            resolvedFont);
    }
}
