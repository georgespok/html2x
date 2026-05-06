using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public sealed class TextRunLayoutTests
{
    [Fact]
    public void Layout_TextRuns_GroupsSequentialRunsIntoOneTextItem()
    {
        var source = new InlineBox(BoxRole.Inline);
        var resolvedFont = new ResolvedFont("Arial", FontWeight.W400, FontStyle.Normal, "test://arial");
        var line = new TextLayoutLine(
            [
                CreateRun(source, "A", 5f, 1f, 2f, resolvedFont),
                CreateRun(source, "B", 6f, 3f, 4f, resolvedFont)
            ],
            21f,
            12f);
        var layout = new TextRunLayout(
            new(
                (_, _, _, _, _, _) => throw new InvalidOperationException("Unexpected inline object."),
                new()),
            new());

        var items = layout.Layout(
            line,
            new(
                10f,
                20f,
                12f,
                29f,
                10f),
            InlineJustificationRules.CreateSequentialTextPlacements);

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
        ResolvedFont? resolvedFont = null) =>
        new(
            source,
            text,
            new("Arial", FontWeight.W400, FontStyle.Normal),
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