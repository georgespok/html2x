using System.Linq;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public class TextLayoutEngineTests
{
    [Fact]
    public void Layout_SingleLine_PreservesRunOrder()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(
            500f,
            12f,
            Run(1, "This is "),
            Run(2, "bold"),
            Run(3, " text."));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(1);
        var line = result.Lines[0];
        line.Runs.Count.ShouldBe(3);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("This is bold text.");
        line.LineHeight.ShouldBe(12f);
        line.LineWidth.ShouldBe(180f);
    }

    [Fact]
    public void Layout_WrapsAtWhitespaceWhenPossible()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(80f, 12f, Run(1, "alpha beta gamma"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBeGreaterThan(1);
        result.Lines[0].Runs.Single().Text.ShouldBe("alpha");
        result.Lines[1].Runs.Single().Text.ShouldBe("beta");
    }

    [Fact]
    public void Layout_LineBreakRun_ForcesNewLine()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(
            500f,
            12f,
            Run(1, "first"),
            LineBreak(2),
            Run(3, "second"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(2);
        result.Lines[0].Runs.Single().Text.ShouldBe("first");
        result.Lines[1].Runs.Single().Text.ShouldBe("second");
    }

    [Fact]
    public void Layout_TrimsTrailingWhitespace()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(500f, 12f, Run(1, "alpha "));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(1);
        result.Lines[0].Runs.Single().Text.ShouldBe("alpha");
    }

    [Fact]
    public void Layout_LongToken_UsesGraphemeFallback()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(15f, 12f, Run(1, "ABCDE"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(5);
        result.Lines.All(line => line.LineWidth <= 15f).ShouldBeTrue();
        string.Concat(result.Lines.SelectMany(line => line.Runs).Select(r => r.Text)).ShouldBe("ABCDE");
    }

    private static TextLayoutInput BuildInput(float width, float lineHeight, params TextRunInput[] runs)
    {
        return new TextLayoutInput(runs, width, lineHeight);
    }

    private static TextRunInput Run(int runId, string text)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new TextRunInput(
            runId,
            new InlineBox { TextContent = text, Style = style },
            text,
            new FontKey("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            PaddingLeft: 0f,
            PaddingRight: 0f,
            MarginLeft: 0f,
            MarginRight: 0f);
    }

    private static TextRunInput LineBreak(int runId)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new TextRunInput(
            runId,
            new InlineBox { TextContent = string.Empty, Style = style },
            string.Empty,
            new FontKey("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            PaddingLeft: 0f,
            PaddingRight: 0f,
            MarginLeft: 0f,
            MarginRight: 0f,
            IsLineBreak: true);
    }
}
