using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;
using Xunit.Abstractions;


namespace Html2x.Test.Scenarios
{
    public class HtmlRenderedToPdfTests(ITestOutputHelper output) : IntegrationTestBase(output)
    {

        public HtmlConverterOptions DefaultOptions => new()
        {
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            },
            Pdf = new PdfOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf"),
                EnableDebugging = true
            }
        };

        [Fact]
        public async Task BoldItalicUnderlineText_ShouldRenderProperly()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                    This is <b>bold</b> text. This is <i>italic</i> text. 
                    This is <u>underlined</u> text
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);

            await SavePdfForInspectionAsync(result.PdfBytes);

            var page = GetLayoutPageSnapshot(result);

            Assert.NotNull(page);

            page.Fragments.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task InlineBlock_MixedAndNestedContent_ShouldPreserveScenarioTextOrder()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                  <body style='margin:0'>
                    <div style='width:360pt'>
                      <span style='display:inline-block; border:1pt solid #000; padding:2pt;'>
                        outer-before
                        <span style='display:inline-block; border:1pt solid #000; padding:1pt;'>
                          inner-before
                          <div>inner-block</div>
                          inner-after
                        </span>
                        outer-after
                      </span>
                    </div>
                  </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);
            var page = GetLayoutPageSnapshot(result);

            var orderedTexts = EnumerateText(page.Fragments)
                .Select(static text => text.Trim())
                .Where(static text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            var outerBefore = orderedTexts.IndexOf("outer-before");
            var innerBefore = orderedTexts.IndexOf("inner-before");
            var innerBlock = orderedTexts.IndexOf("inner-block");
            var innerAfter = orderedTexts.IndexOf("inner-after");
            var outerAfter = orderedTexts.IndexOf("outer-after");

            outerBefore.ShouldBeGreaterThanOrEqualTo(0);
            innerBefore.ShouldBeGreaterThan(outerBefore);
            innerBlock.ShouldBeGreaterThan(innerBefore);
            innerAfter.ShouldBeGreaterThan(innerBlock);
            outerAfter.ShouldBeGreaterThan(innerAfter);
        }

        [Fact]
        public async Task SharedFormattingScenario_TopLevelAndInlineBlock_ShouldProduceParityMetrics()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                  <body style='margin:0'>
                    <div style='border:1pt solid #000; padding:2pt; width:220pt'>
                      <div style='height:10pt; padding-bottom:5pt; border-bottom:3pt solid #000; margin-bottom:12pt;'>top-first</div>
                      <div style='height:8pt; margin-top:4pt;'>top-second</div>
                    </div>
                    <div style='height:10pt'></div>
                    <div style='width:220pt'>
                      <span style='display:inline-block; border:1pt solid #000; padding:2pt; width:220pt'>
                        <div style='height:10pt; padding-bottom:5pt; border-bottom:3pt solid #000; margin-bottom:12pt;'>inline-first</div>
                        <div style='height:8pt; margin-top:4pt;'>inline-second</div>
                      </span>
                    </div>
                  </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);
            var page = GetLayoutPageSnapshot(result);

            var topContainer = FindContainerWithTexts(page.Fragments, "top-first", "top-second");
            var inlineContainer = FindContainerWithTexts(page.Fragments, "inline-first", "inline-second");

            var topMetrics = ExtractTwoLineMetrics(topContainer, "top-first", "top-second");
            var inlineMetrics = ExtractTwoLineMetrics(inlineContainer, "inline-first", "inline-second");

            inlineMetrics.ContainerHeight.ShouldBe(topMetrics.ContainerHeight, 0.5f);
            inlineMetrics.FirstLineHeight.ShouldBe(topMetrics.FirstLineHeight, 0.5f);
            inlineMetrics.SecondLineHeight.ShouldBe(topMetrics.SecondLineHeight, 0.5f);
            topMetrics.LineDeltaY.ShouldBeGreaterThan(0f);
            inlineMetrics.LineDeltaY.ShouldBeGreaterThan(0f);
        }

        private static LayoutPageSnapshot GetLayoutPageSnapshot(Html2PdfResult result)
        {
            var endLayoutBuild = result.Diagnostics!.Events.FirstOrDefault(x => x is { Type: DiagnosticsEventType.EndStage, Payload: not null, Name: "LayoutBuild" });

            var snapshot = ((LayoutSnapshotPayload) endLayoutBuild!.Payload!).Snapshot;

            return snapshot.Pages[0];
        }

        private static IEnumerable<string> EnumerateText(IReadOnlyList<FragmentSnapshot> fragments)
        {
            foreach (var fragment in fragments)
            {
                if (!string.IsNullOrWhiteSpace(fragment.Text))
                {
                    yield return fragment.Text!;
                }

                foreach (var childText in EnumerateText(fragment.Children))
                {
                    yield return childText;
                }
            }
        }

        private static FragmentSnapshot FindContainerWithTexts(
            IReadOnlyList<FragmentSnapshot> fragments,
            string firstText,
            string secondText)
        {
            if (TryFindContainerWithTexts(fragments, firstText, secondText, out var container))
            {
                return container;
            }

            throw new ShouldAssertException(
                $"Expected container with texts '{firstText}' and '{secondText}'.");
        }

        private static bool TryFindContainerWithTexts(
            IReadOnlyList<FragmentSnapshot> fragments,
            string firstText,
            string secondText,
            out FragmentSnapshot container)
        {
            foreach (var fragment in fragments)
            {
                if (ContainsText(fragment, firstText) && ContainsText(fragment, secondText))
                {
                    container = fragment;
                    return true;
                }

                if (fragment.Children.Count > 0 &&
                    TryFindContainerWithTexts(fragment.Children, firstText, secondText, out container))
                {
                    return true;
                }
            }

            container = default!;
            return false;
        }

        private static bool ContainsText(FragmentSnapshot fragment, string text)
        {
            if (!string.IsNullOrWhiteSpace(fragment.Text) &&
                fragment.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var child in fragment.Children)
            {
                if (ContainsText(child, text))
                {
                    return true;
                }
            }

            return false;
        }

        private static (float ContainerHeight, float FirstLineHeight, float SecondLineHeight, float LineDeltaY) ExtractTwoLineMetrics(
            FragmentSnapshot container,
            string firstText,
            string secondText)
        {
            var lines = EnumerateLines(container).ToList();
            lines.ShouldNotBeEmpty();

            var firstLine = lines.First(line =>
                !string.IsNullOrWhiteSpace(line.Text) &&
                line.Text.Contains(firstText, StringComparison.OrdinalIgnoreCase));
            var secondLine = lines.First(line =>
                !string.IsNullOrWhiteSpace(line.Text) &&
                line.Text.Contains(secondText, StringComparison.OrdinalIgnoreCase));

            return (
                container.Size.Height,
                firstLine.Size.Height,
                secondLine.Size.Height,
                secondLine.Y - firstLine.Y);
        }

        private static IEnumerable<FragmentSnapshot> EnumerateLines(FragmentSnapshot fragment)
        {
            if (string.Equals(fragment.Kind, "line", StringComparison.Ordinal))
            {
                yield return fragment;
            }

            foreach (var child in fragment.Children)
            {
                foreach (var nested in EnumerateLines(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
