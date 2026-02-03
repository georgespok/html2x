using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.Abstractions.Diagnostics;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Display;

public class BlockFlowTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

    [Fact]
    public async Task BlockStacking_UsesCollapsedMarginsAndIncludesPaddingInHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 10pt; padding-top: 4pt; border-top: 2pt solid #000; height: 20pt;'>A</div>
                <div style='margin-top: 6pt; height: 10pt;'>B</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[0].Children[1];

        var expectedFirstHeight = 20f + 4f + 2f;
        first.Rect.Height.ShouldBe(expectedFirstHeight, 0.5f);

        var expectedGap = 10f; // collapsed max(10pt, 6pt)
        var expectedSecondY = first.Rect.Y + first.Rect.Height + expectedGap;
        second.Rect.Y.ShouldBe(expectedSecondY, 0.5f);
    }

    [Fact]
    public async Task AdjacentBlocks_CollapseMarginsToMaxValue()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 20pt; height: 10pt;'>First</div>
                <div style='margin-top: 6pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var first = (BlockFragment)layout.Pages[0].Children[0];
        var second = (BlockFragment)layout.Pages[0].Children[1];

        var expectedGap = 20f;
        var expectedSecondY = first.Rect.Y + first.Rect.Height + expectedGap;

        second.Rect.Y.ShouldBe(expectedSecondY, 0.5f);
    }

    [Fact]
    public async Task ParentAndChildMargins_DoNotCollapse()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-top: 12pt;'>
                  <p style='margin-top: 8pt; height: 10pt;'>Child</p>
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var parent = (BlockFragment)layout.Pages[0].Children[0];
        var child = parent.Children.OfType<BlockFragment>().First();

        var expectedChildY = parent.Rect.Y + 8f;
        child.Rect.Y.ShouldBe(expectedChildY, 0.5f);
    }

    [Fact]
    public async Task MarginCollapse_EmitsDiagnosticsEvent()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: 20pt; height: 10pt;'>First</div>
                <div style='margin-top: 6pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var diagnosticsSession = new DiagnosticsSession
        {
            Options = new HtmlConverterOptions()
        };

        var layoutBuilder = CreateLayoutBuilder(CreateLinearMeasurer(10f));
        _ = await layoutBuilder.BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.A4 }, diagnosticsSession);

        var marginEvents = diagnosticsSession.Events
            .Where(e => e.Payload is MarginCollapsePayload)
            .Select(e => (MarginCollapsePayload)e.Payload!)
            .ToList();

        marginEvents.ShouldNotBeEmpty();
        var match = marginEvents.FirstOrDefault(e =>
            Math.Abs(e.PreviousBottomMargin - 20f) < 0.01f &&
            Math.Abs(e.NextTopMargin - 6f) < 0.01f &&
            Math.Abs(e.CollapsedTopMargin - 20f) < 0.01f);

        match.ShouldNotBeNull();
    }

    [Fact]
    public async Task NegativeMargins_ClampToParentContentBox()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='margin-bottom: -20pt; height: 0;'>First</div>
                <div style='margin-top: -30pt; margin-left: -25pt; height: 10pt;'>Second</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var second = (BlockFragment)layout.Pages[0].Children[1];

        second.Rect.X.ShouldBeGreaterThanOrEqualTo(0f);
        second.Rect.Y.ShouldBeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public async Task PaddingAndBorder_AffectBlockHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='height: 12pt; padding-top: 5pt; padding-bottom: 7pt; border-top: 2pt solid #000; border-bottom: 3pt solid #000;'>Box</div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var expectedHeight = 12f + 5f + 7f + 2f + 3f;

        block.Rect.Height.ShouldBe(expectedHeight, 0.5f);
    }

    [Fact]
    public async Task MixedInlineAndBlock_CreatesBlockToInlineBoundary()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                  Inline text
                  <div style='height: 10pt;'>Block</div>
                </div>
              </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var container = (BlockFragment)layout.Pages[0].Children[0];
        var blockChildren = container.Children.OfType<BlockFragment>().ToList();
        blockChildren.Count.ShouldBeGreaterThanOrEqualTo(2);

        var inlineIndex = blockChildren.FindIndex(b =>
            b.Children.OfType<LineBoxFragment>()
                .SelectMany(line => line.Runs)
                .Any(run => run.Text.Contains("Inline", StringComparison.OrdinalIgnoreCase)));
        var blockIndex = blockChildren.FindIndex(b =>
            b.Children.OfType<LineBoxFragment>()
                .SelectMany(line => line.Runs)
                .Any(run => run.Text.Contains("Block", StringComparison.OrdinalIgnoreCase)));

        inlineIndex.ShouldBeGreaterThanOrEqualTo(0);
        blockIndex.ShouldBeGreaterThanOrEqualTo(0);
        inlineIndex.ShouldBeLessThan(blockIndex);
    }

    private static async Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer)
    {
        return await Fixture.BuildLayoutAsync(html, textMeasurer, new LayoutOptions
        {
            PageSize = PaperSizes.A4
        });
    }

    private static LayoutBuilder CreateLayoutBuilder(ITextMeasurer textMeasurer)
    {
        var services = new LayoutServices(
            textMeasurer,
            LayoutBuilderFixture.CreateFontSource(),
            new NoopImageProvider());

        return new LayoutBuilderFactory().Create(services);
    }

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey _, float _, string text) => text.Length * widthPerChar);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((8f, 2f));
        return textMeasurer.Object;
    }
}
