using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.Abstractions.Diagnostics;
using Moq;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Display;

public class InlineBlockTests
{
    [Fact]
    public async Task InlineBlock_ShouldEmitBlockFragmentWithBorders()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div id='container' style='width: 200pt; border: 1pt solid #000; padding: 4pt;'>
                  <div id='inline-block-atomic'><strong>Inline-block atomic:</strong></div>
                  <span id='inline-block-a' style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Inline-block A</span>
                  <span id='inline-block-b' style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Inline-block B</span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var root = (BlockFragment)layout.Pages[0].Children[0];
        var inlineBlock = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .FirstOrDefault(fragment => fragment.Style?.Borders?.HasAny == true);

        inlineBlock.ShouldNotBeNull();
    }

    [Fact]
    public async Task InlineBlock_TextShouldNotBeFlattenedIntoParentLine()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div id='container' style='width: 200pt;'>
                  <div id='inline-block-atomic'><strong>Inline-block atomic:</strong></div>
                  <span id='inline-block-a' style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Inline-block A</span>
                  <span id='inline-block-b' style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Inline-block B</span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));

        var root = (BlockFragment)layout.Pages[0].Children[0];
        var parentLineText = root.Children.OfType<LineBoxFragment>()
            .SelectMany(line => line.Runs)
            .Select(run => run.Text)
            .ToList();

        parentLineText.ShouldNotContain("Inline-block A");
        parentLineText.ShouldNotContain("Inline-block B");
    }

    [Fact]
    public async Task InlineBlock_BlockDescendants_ShouldContributeTextLines()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div id='container' style='width: 300pt;'>
                  <span id='inline-block-a' style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    <div>First block line</div>
                    <div>Second block line</div>
                  </span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));

        var root = (BlockFragment)layout.Pages[0].Children[0];
        var inlineBlockTexts = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Select(run => run.Text)
            .ToList();

        inlineBlockTexts.ShouldContain("First block line");
        inlineBlockTexts.ShouldContain("Second block line");
    }

    [Fact]
    public async Task InlineBlock_NestedInlineBlock_ShouldKeepNestedText()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div id='container' style='width: 300pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    outer
                    <span style='display: inline-block; border: 1pt solid #000; padding: 1pt;'>inner nested text</span>
                  </span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var allTexts = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Select(run => run.Text)
            .ToList();

        allTexts.ShouldContain("inner nested text");
    }

    [Fact]
    public async Task InlineBlock_NestedInlineBlock_ShouldPreserveTextOrderAcrossBoundaries()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 360pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    outer-before
                    <span style='display: inline-block; border: 1pt solid #000; padding: 1pt;'>
                      inner-before
                      <div>inner-block</div>
                      inner-after
                    </span>
                    outer-after
                  </span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var orderedTexts = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Select(run => run.Text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        orderedTexts.ShouldBe(["outer-before", "inner-before", "inner-block", "inner-after", "outer-after"]);
    }

    [Fact]
    public async Task InlineBlock_MixedInlineAndBlockDescendants_ShouldPreserveAllText()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 320pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    inline-before
                    <div>
                      <div>block-one</div>
                    </div>
                    inline-middle
                    <ul>
                      <li>block-two</li>
                    </ul>
                    inline-after
                  </span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var texts = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Select(run => run.Text)
            .ToList();

        var normalizedTexts = texts
            .Select(text => text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text) && !string.Equals(text, "•", StringComparison.Ordinal))
            .ToList();

        normalizedTexts.ShouldBe(["inline-before", "block-one", "inline-middle", "block-two", "inline-after"]);
    }

    [Fact]
    public async Task InlineBlock_MixedInlineAndBlockDescendants_ShouldNotUndercountHeight()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 280pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    Alpha inline-block
                    <div>Third block descendant with</div>
                    suffix text
                  </span>
                  <div>First block</div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];

        var inlineBlock = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .FirstOrDefault(fragment =>
                fragment.Style?.Borders?.HasAny == true &&
                ContainsLineText(fragment, "Alpha inline-block") &&
                ContainsLineText(fragment, "Third block descendant with") &&
                ContainsLineText(fragment, "suffix text"));
        inlineBlock.ShouldNotBeNull();

        var inlineBlockLines = EnumerateFragments(inlineBlock)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .ToList();

        var alphaLine = inlineBlockLines.First(line => line.Runs.Any(run => run.Text.Contains("Alpha inline-block", StringComparison.OrdinalIgnoreCase)));
        var descendantLine = inlineBlockLines.First(line => line.Runs.Any(run => run.Text.Contains("Third block descendant with", StringComparison.OrdinalIgnoreCase)));
        var suffixLine = inlineBlockLines.First(line => line.Runs.Any(run => run.Text.Contains("suffix text", StringComparison.OrdinalIgnoreCase)));

        descendantLine.Rect.Y.ShouldBeGreaterThanOrEqualTo(alphaLine.Rect.Bottom - 0.1f);
        suffixLine.Rect.Y.ShouldBeGreaterThanOrEqualTo(descendantLine.Rect.Bottom - 0.1f);
        inlineBlock.Rect.Bottom.ShouldBeGreaterThanOrEqualTo(suffixLine.Rect.Bottom - 0.1f);
    }

    [Fact]
    public async Task InlineBlock_UnsupportedInternalStructure_ShouldFailLayoutAndEmitDiagnostics()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 320pt;'>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    <table>
                      <tr><td>unsupported</td></tr>
                    </table>
                  </span>
                </div>
              </body>
            </html>";

        var diagnosticsSession = new DiagnosticsSession
        {
            Options = new HtmlConverterOptions()
        };

        var layoutBuilder = CreateLayoutBuilder(InlineFlowTestHelpers.CreateLinearMeasurer(6f));

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await layoutBuilder.BuildAsync(html, new LayoutOptions { PageSize = PaperSizes.A4 }, diagnosticsSession));

        diagnosticsSession.Events
            .Any(e => e.Payload is UnsupportedStructurePayload payload &&
                      payload.FormattingContext == FormattingContextKind.InlineBlock)
            .ShouldBeTrue();
    }

    private static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
    {
        yield return fragment;
        if (fragment is BlockFragment block)
        {
            foreach (var child in block.Children)
            {
                foreach (var nested in EnumerateFragments(child))
                {
                    yield return nested;
                }
            }
        }
    }

    private static LayoutBuilder CreateLayoutBuilder(ITextMeasurer textMeasurer)
    {
        var services = new LayoutServices(
            textMeasurer,
            LayoutBuilderFixture.CreateFontSource(),
            new NoopImageProvider());

        return new LayoutBuilderFactory().Create(services);
    }

    private static bool ContainsLineText(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}
