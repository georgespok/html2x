using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Geometry.Text;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Text;
using Html2x.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public class TextLineLayoutTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

    [Fact]
    public void Layout_SingleLine_PreservesRunOrder()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
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
    public void Layout_TextWithSpaces_WrapsAtWhitespace()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(80f, 12f, Run(1, "alpha beta gamma"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBeGreaterThan(1);
        result.Lines[0].Runs.Single().Text.ShouldBe("alpha");
        result.Lines[1].Runs.Single().Text.ShouldBe("beta");
    }

    [Fact]
    public void Layout_LineBreakRun_ForcesNewLine()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
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
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(500f, 12f, Run(1, "alpha "));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(1);
        result.Lines[0].Runs.Single().Text.ShouldBe("alpha");
    }

    [Fact]
    public void Layout_LongToken_UsesGraphemeFallback()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(15f, 12f, Run(1, "ABCDE"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(5);
        result.Lines.All(line => line.LineWidth <= 15f).ShouldBeTrue();
        string.Concat(result.Lines.SelectMany(line => line.Runs).Select(r => r.Text)).ShouldBe("ABCDE");
    }

    [Fact]
    public void Layout_ZeroWidth_DoesNotTreatTextAsUnbounded()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(0f, 12f, Run(1, "ABC"));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(3);
        result.Lines.SelectMany(static line => line.Runs).Select(static run => run.Text)
            .ShouldBe(["A", "B", "C"]);
    }

    [Fact]
    public void Layout_InlineBox_WrapsAndUsesInlineBoxMetrics()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var inlineBox = CreateInlineBox(25f, 18f, 13f);
        var input = BuildInput(
            50f,
            12f,
            Run(1, "alpha"),
            InlineBoxRun(2, inlineBox));

        var result = engine.Layout(input);

        result.Lines.Count.ShouldBe(2);
        result.Lines[0].Runs.Single().Text.ShouldBe("alpha");
        var objectLine = result.Lines[1];
        objectLine.LineHeight.ShouldBe(18f);
        objectLine.LineWidth.ShouldBe(25f);
        var objectRun = objectLine.Runs.Single();
        objectRun.Text.ShouldBeEmpty();
        objectRun.Width.ShouldBe(25f);
        objectRun.Ascent.ShouldBe(13f);
        objectRun.Descent.ShouldBe(5f);
        objectRun.InlineBox.ShouldBeSameAs(inlineBox);
    }

    [Fact]
    public void Layout_WrappedRun_PreservesResolvedFontOnEachProducedRun()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);
        var input = BuildInput(60f, 12f, Run(1, "alpha beta", font));

        var result = engine.Layout(input);

        var producedRuns = result.Lines.SelectMany(static line => line.Runs).ToList();
        producedRuns.Select(static run => run.Text).ShouldBe(["alpha", "beta"]);
        producedRuns.All(static run => run.ResolvedFont is not null).ShouldBeTrue();
        producedRuns.Select(static run => run.ResolvedFont!.SourceId)
            .ShouldAllBe(static sourceId => sourceId == "fallback://Inter/W400/Normal");
    }

    [Fact]
    public void Layout_NegativeWidth_Throws()
    {
        var engine = new TextLineLayout(new FakeTextMeasurer(10f, 9f, 3f));
        var input = BuildInput(-1f, 12f, Run(1, "ABC"));

        Should.Throw<ArgumentOutOfRangeException>(() => engine.Layout(input));
    }

    [Fact]
    public void Layout_RepeatedRunsWithSameFont_ReusesResolvedFontPath()
    {
        var measurer = new CachingResolvedFontMeasurer(10f, 9f, 3f);
        var font = new FontKey("Inter", FontWeight.W400, FontStyle.Normal);
        var engine = new TextLineLayout(measurer);
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
                new() { PageSize = PaperSizes.A4 });
            var snapshot = LayoutSnapshotMapper.From(layout);
            snapshots.Add(snapshot);
            var signatures = Flatten(snapshot.Pages[0].Fragments).ToList();
            runs.Add(signatures);
        }

        runs[1].ShouldBe(runs[0]);
        runs[2].ShouldBe(runs[0]);
        AssertSequenceIdsAreMonotonic(snapshots[0].Pages[0].Fragments);
    }

    private static TextLayoutInput BuildInput(float width, float lineHeight, params TextRunInput[] runs) =>
        new(runs, width, lineHeight);

    private static TextRunInput Run(int runId, string text, FontKey? font = null)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new(
            runId,
            new(BoxRole.Inline) { TextContent = text, Style = style },
            text,
            font ?? new FontKey("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            0f,
            0f,
            0f,
            0f);
    }

    private static TextRunInput InlineBoxRun(int runId, InlineBoxLayout inlineBox)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new(
            runId,
            new(BoxRole.InlineBlock) { TextContent = string.Empty, Style = style },
            string.Empty,
            new("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            0f,
            0f,
            0f,
            0f,
            TextRunKind.InlineBox,
            inlineBox);
    }

    private static TextRunInput LineBreak(int runId)
    {
        var style = new ComputedStyle { FontSizePt = 12 };
        return new(
            runId,
            new(BoxRole.Inline) { TextContent = string.Empty, Style = style },
            string.Empty,
            new("Default", FontWeight.W400, FontStyle.Normal),
            12f,
            style,
            0f,
            0f,
            0f,
            0f,
            TextRunKind.LineBreak);
    }

    private static InlineBoxLayout CreateInlineBox(float width, float height, float baseline)
    {
        var contentBox = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };

        return new(
            contentBox,
            new([], 0f, 0f),
            width,
            height,
            width,
            height,
            baseline);
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
                resolvedFont = new(
                    font.Family,
                    font.Weight,
                    font.Style,
                    $"test://{font.Family}/{font.Weight}/{font.Style}");
                _resolvedFonts.Add(font, resolvedFont);
                ResolutionCount++;
            }

            return new(
                MeasureWidth(font, sizePt, text),
                ascent,
                descent,
                resolvedFont);
        }

        public float MeasureWidth(FontKey font, float sizePt, string text) =>
            string.IsNullOrEmpty(text) ? 0f : text.Length * widthPerChar;

        public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt) => (ascent, descent);
    }
}
