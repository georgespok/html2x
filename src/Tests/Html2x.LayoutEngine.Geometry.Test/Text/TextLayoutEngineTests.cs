using Html2x.LayoutEngine.Style;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Text;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Text;

public class TextLayoutEngineTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

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

    [Fact]
    public void Layout_ZeroWidth_DoesNotTreatTextAsUnbounded()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(0f, 12f, Run(1, "ABC"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(3);
        result.Lines.SelectMany(static line => line.Runs).Select(static run => run.Text)
            .ShouldBe(["A", "B", "C"]);
    }

    [Fact]
    public void Layout_NegativeWidth_Throws()
    {
        var engine = new TextLayoutEngine(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(-1f, 12f, Run(1, "ABC"));

        Should.Throw<ArgumentOutOfRangeException>(() => engine.Layout(input));
    }

    [Fact]
    public void Layout_RepeatedRunsWithSameFont_ReusesResolvedFontPath()
    {
        var measurer = new CachingResolvedFontMeasurer(10f, 9f, 3f);
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);
        var engine = new TextLayoutEngine(measurer);
        var input = BuildInput(
            500f,
            12f,
            Run(1, "alpha", font),
            Run(2, "beta", font));

        var result = engine.Layout(input);

        var resolvedSourceIds = result.Lines
            .SelectMany(static line => line.Runs)
            .Select(static run => run.ResolvedFont.ShouldNotBeNull().SourceId)
            .Distinct()
            .ToList();
        resolvedSourceIds.ShouldHaveSingleItem().ShouldBe("test://Inter/W400/Normal");
        measurer.ResolutionCount.ShouldBe(1);
    }

    [Fact]
    public async Task SnapshotFragmentOrdering_RepeatedRuns_PreservesOrderAndIds()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                  Lead
                  <div style='height: 10pt;'>Child</div>
                </div>
              </body>
            </html>";

        var runs = new List<IReadOnlyList<string>>();
        var snapshots = new List<LayoutSnapshot>();
        for (var iteration = 0; iteration < 3; iteration++)
        {
            var layout = await Fixture.BuildLayoutAsync(
                html,
                new FakeTextMeasurer(10f, 9f, 3f),
                new LayoutBuildSettings { PageSize = PaperSizes.A4 });
            var snapshot = LayoutSnapshotMapper.From(layout);
            snapshots.Add(snapshot);
            var signatures = Flatten(snapshot.Pages[0].Fragments).ToList();
            runs.Add(signatures);
        }

        runs[1].ShouldBe(runs[0]);
        runs[2].ShouldBe(runs[0]);
        AssertSequenceIdsAreMonotonic(snapshots[0].Pages[0].Fragments);
    }

    private static TextLayoutInput BuildInput(float width, float lineHeight, params TextRunInput[] runs)
    {
        return new TextLayoutInput(runs, width, lineHeight);
    }

    private static TextRunInput Run(int runId, string text, FontKey? font = null)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new TextRunInput(
            runId,
            new InlineBox(BoxRole.Inline) { TextContent = text, Style = style },
            text,
            font ?? new FontKey("Default", FontWeight.W400, FontStyle.Normal),
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
            new InlineBox(BoxRole.Inline) { TextContent = string.Empty, Style = style },
            string.Empty,
            new FontKey("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            PaddingLeft: 0f,
            PaddingRight: 0f,
            MarginLeft: 0f,
            MarginRight: 0f,
            Kind: TextRunKind.LineBreak);
    }

    private static IEnumerable<string> Flatten(IReadOnlyList<FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            var text = fragment.Text ?? string.Empty;
            yield return $"{fragment.SequenceId}|{fragment.Kind}|{fragment.Y:F3}|{fragment.X:F3}|{text}";

            foreach (var child in Flatten(fragment.Children))
            {
                yield return child;
            }
        }
    }

    private static void AssertSequenceIdsAreMonotonic(IReadOnlyList<FragmentSnapshot> fragments)
    {
        var flattened = FlattenSnapshots(fragments)
            .ToList();

        flattened.Select(static snapshot => snapshot.SequenceId)
            .ShouldBe(Enumerable.Range(1, flattened.Count).ToList());
    }

    private static IEnumerable<FragmentSnapshot> FlattenSnapshots(IReadOnlyList<FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            yield return fragment;

            foreach (var child in FlattenSnapshots(fragment.Children))
            {
                yield return child;
            }
        }
    }

    private sealed class CachingResolvedFontMeasurer(float widthPerChar, float ascent, float descent) : ITextMeasurer
    {
        private readonly Dictionary<FontKey, ResolvedFont> _resolvedFonts = [];

        public int ResolutionCount { get; private set; }

        public TextMeasurement Measure(FontKey font, float sizePt, string text)
        {
            if (!_resolvedFonts.TryGetValue(font, out var resolvedFont))
            {
                resolvedFont = new ResolvedFont(
                    font.Family,
                    font.Weight,
                    font.Style,
                    $"test://{font.Family}/{font.Weight}/{font.Style}");
                _resolvedFonts.Add(font, resolvedFont);
                ResolutionCount++;
            }

            return new TextMeasurement(
                MeasureWidth(font, sizePt, text),
                ascent,
                descent,
                resolvedFont);
        }

        public float MeasureWidth(FontKey font, float sizePt, string text)
        {
            return string.IsNullOrEmpty(text) ? 0f : text.Length * widthPerChar;
        }

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
        {
            return (ascent, descent);
        }
    }
}