using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Test.RenderModel;

public sealed class RenderModelImmutabilityTests
{
    [Fact]
    public void LayoutPage_ChildrenInputMutatesAfterConstruction_KeepsOriginalChildren()
    {
        var first = new BlockFragment();
        var children = new List<Fragment> { first };

        var page = new LayoutPage(PaperSizes.A4, new(), children);

        children.Add(new BlockFragment());

        page.Children.ShouldHaveSingleItem().ShouldBeSameAs(first);
    }

    [Fact]
    public void LineBoxFragment_RunsInputMutatesAfterConstruction_KeepsOriginalRuns()
    {
        var first = CreateTextRun("alpha");
        var runs = new List<TextRun> { first };

        var line = new LineBoxFragment
        {
            Runs = runs
        };

        runs.Add(CreateTextRun("beta"));

        line.Runs.ShouldHaveSingleItem().ShouldBeSameAs(first);
    }

    [Fact]
    public void HtmlLayout_PagesInputMutatesAfterConstruction_KeepsOriginalPages()
    {
        var first = new LayoutPage(PaperSizes.A4, new(), []);
        var pages = new List<LayoutPage> { first };

        var layout = new HtmlLayout(pages);

        pages.Add(new(PaperSizes.A4, new(), []));

        layout.Pages.ShouldHaveSingleItem().ShouldBeSameAs(first);
    }

    private static TextRun CreateTextRun(string text) =>
        new(
            text,
            new("Inter", FontWeight.W400, FontStyle.Normal),
            12f,
            new(0f, 0f),
            12f,
            9f,
            3f);
}