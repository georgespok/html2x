using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
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
        public async Task BoldItalicUnderlineText_Render()
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
        public async Task BaselineRenderingSmoke_RichContent_ProducesPdfAndSnapshot()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                  <body style='margin:0'>
                    <div style='background-color:#eef2ff; border:1px solid #111827; padding:4px;'>
                      baseline text
                    </div>
                    <img src='missing-baseline-image.png' width='16' height='16' style='display:block; border:1px solid #222;' />
                    <table style='width:120px; border:1px solid #333;'>
                      <tr>
                        <td style='border:1px solid #444; background-color:#fef3c7;'>cell</td>
                      </tr>
                    </table>
                  </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);
            var page = GetLayoutPageSnapshot(result);
            var fragments = Flatten(page.Fragments).ToList();
            var text = EnumerateText(page.Fragments).ToList();

            result.PdfBytes.ShouldNotBeEmpty();
            text.ShouldContain("baseline text");
            text.ShouldContain("cell");
            fragments.Any(static fragment => fragment.Kind == "image").ShouldBeTrue();
            fragments.Any(static fragment => fragment.Kind == "table").ShouldBeTrue();
            fragments.Any(static fragment => fragment.BackgroundColor != null).ShouldBeTrue();
            fragments.Any(static fragment => fragment.Borders != null && fragment.Borders.HasAny).ShouldBeTrue();
        }

        [Fact]
        public async Task InlineBlock_MixedAndNestedContent_PreserveScenarioTextOrder()
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
        public async Task SharedFormattingScenario_TopLevelAndInlineBlock_ProduceParityMetrics()
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

        [Fact]
        public async Task SharedFormattingInlineBlockCases_PreserveTextOrderInLayoutSnapshot()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                  <body style='margin:0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                    <div style='width:320pt; margin-bottom: 12pt;'>
                      Prefix text
                      <span style='display:inline-block; vertical-align:top; width:190pt; border:1pt solid #222; padding:6pt; margin-right:10pt;'>
                        Alpha inline-block
                        <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>First block descendant</div>
                        <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>
                          Third block descendant with
                          <span style='display:inline-block; margin-top:4pt; padding:4pt; border:1pt solid #777;'>
                            nested inline-block
                            <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>Nested block descendant A</div>
                          </span>
                        </div>
                      </span>
                      suffix text
                    </div>
                    <div style='width:320pt;'>
                      text-before
                      <span style='display:inline-block; vertical-align:top; width:190pt; border:1pt solid #222; padding:6pt; margin-right:10pt;'>
                        outer-start
                        <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>block-child-1</div>
                        <span style='display:inline-block; margin-top:4pt; padding:4pt; border:1pt solid #777;'>
                          nested-inline-start
                          <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>nested-block-1</div>
                          <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>nested-block-2</div>
                        </span>
                        <div style='display:block; margin:5pt 0; padding:3pt 4pt; border:1pt dashed #888;'>block-child-2</div>
                        outer-end
                      </span>
                      text-after
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

            AssertIncreasingOrder(orderedTexts, [
                "Prefix text",
                "Alpha inline-block",
                "First block descendant",
                "Third block descendant with",
                "nested inline-block",
                "Nested block descendant A",
                "suffix text"
            ]);

            AssertIncreasingOrder(orderedTexts, [
                "text-before",
                "outer-start",
                "block-child-1",
                "nested-inline-start",
                "nested-block-1",
                "nested-block-2",
                "block-child-2",
                "outer-end",
                "text-after"
            ]);
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

        private static IEnumerable<FragmentSnapshot> Flatten(IReadOnlyList<FragmentSnapshot> fragments)
        {
            foreach (var fragment in fragments)
            {
                yield return fragment;

                foreach (var child in Flatten(fragment.Children))
                {
                    yield return child;
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

        private static void AssertIncreasingOrder(IReadOnlyList<string> orderedTexts, IReadOnlyList<string> expectedSequence)
        {
            var lastIndex = -1;
            foreach (var expected in expectedSequence)
            {
                var currentIndex = orderedTexts
                    .Select((text, index) => (text, index))
                    .First(tuple => string.Equals(tuple.text, expected, StringComparison.Ordinal))
                    .index;
                currentIndex.ShouldBeGreaterThan(lastIndex);
                lastIndex = currentIndex;
            }
        }
    }
}
