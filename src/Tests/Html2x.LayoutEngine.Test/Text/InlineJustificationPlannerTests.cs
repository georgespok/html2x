using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Text;
using Html2x.LayoutEngine.Test.TestDoubles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public sealed class InlineJustificationPlannerTests
{
    private readonly InlineJustificationPlanner _planner = new(
        new FakeTextMeasurer(2f, 9f, 3f),
        new InlineAlignmentResolver());

    [Fact]
    public void CreatePlan_JustifiedNonLastLineAndWhitespace_ReturnsPerWhitespaceExtra()
    {
        var line = new TextLayoutLine(
            [CreateRun("Hello world again", width: 70f)],
            LineWidth: 70f,
            LineHeight: 12f);

        var plan = _planner.CreatePlan("justify", 100f, 70f, line, lineIndex: 0, lineCount: 2);

        plan.ShouldJustify.ShouldBeTrue();
        plan.ExtraSpace.ShouldBe(30f);
        plan.PerWhitespaceExtra.ShouldBe(15f);
    }

    [Fact]
    public void CreatePlan_ForLastLine_ReturnsNone()
    {
        var line = new TextLayoutLine(
            [CreateRun("Hello world", width: 50f)],
            LineWidth: 50f,
            LineHeight: 12f);

        var plan = _planner.CreatePlan("justify", 100f, 50f, line, lineIndex: 1, lineCount: 2);

        plan.ShouldJustify.ShouldBeFalse();
    }

    [Fact]
    public void CreateJustifiedTextPlacements_TokenizesAndMeasuresEachToken()
    {
        var run = CreateRun("Hi there", width: 16f, leftSpacing: 2f, rightSpacing: 3f);
        var plan = new JustificationPlan(ShouldJustify: true, ExtraSpace: 4f, PerWhitespaceExtra: 4f);

        var placements = _planner.CreateJustifiedTextPlacements(run, plan);

        placements.Count.ShouldBe(3);
        placements[0].Text.ShouldBe("Hi");
        placements[0].Width.ShouldBe(4f);
        placements[0].LeftSpacing.ShouldBe(2f);
        placements[0].RightSpacing.ShouldBe(0f);
        placements[0].ExtraAfter.ShouldBe(0f);
        placements[1].Text.ShouldBe(" ");
        placements[1].Width.ShouldBe(2f);
        placements[1].ExtraAfter.ShouldBe(4f);
        placements[2].Text.ShouldBe("there");
        placements[2].Width.ShouldBe(10f);
        placements[2].RightSpacing.ShouldBe(3f);
    }

    private static TextLayoutRun CreateRun(
        string text,
        float width,
        float leftSpacing = 0f,
        float rightSpacing = 0f)
    {
        return new TextLayoutRun(
            new InlineBox(DisplayRole.Inline),
            text,
            new FontKey("Arial", FontWeight.W400, FontStyle.Normal),
            12f,
            width,
            leftSpacing,
            rightSpacing,
            9f,
            3f,
            TextDecorations.None,
            ColorRgba.Black);
    }
}
