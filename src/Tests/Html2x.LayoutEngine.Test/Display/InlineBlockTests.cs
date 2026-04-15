using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.Abstractions.Diagnostics;
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
    public async Task InlineBlock_AfterBlockSibling_ShouldStartBelowPreviousBlock()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 200pt; border: 1pt solid #000; padding: 4pt;'>
                  <div><strong>Inline-block atomic:</strong></div>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Box A</span>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>Box B</span>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));

        var root = (BlockFragment)layout.Pages[0].Children[0];
        var heading = FindLine(root, "Inline-block atomic:");
        var inlineBlockA = FindLine(root, "Box A");
        var inlineBlockB = FindLine(root, "Box B");
        var inlineBlockAContainer = FindInlineBlock(root, "Box A");
        var inlineBlockBContainer = FindInlineBlock(root, "Box B");

        inlineBlockA.Rect.Y.ShouldBeGreaterThanOrEqualTo(heading.Rect.Bottom - 0.1f);
        inlineBlockB.Rect.Y.ShouldBeGreaterThanOrEqualTo(heading.Rect.Bottom - 0.1f);
        root.Rect.Bottom.ShouldBeGreaterThanOrEqualTo(inlineBlockAContainer.Rect.Bottom - 0.1f);
        root.Rect.Bottom.ShouldBeGreaterThanOrEqualTo(inlineBlockBContainer.Rect.Bottom - 0.1f);
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
    public async Task InlineBlock_SharedFormattingCase2_ShouldKeepPrefixInlineBlockAndSuffixInSiblingOrder()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    Prefix text
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      Alpha inline-block
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>First block descendant</div>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>Second block descendant</div>
                    </span>
                    suffix text
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var row = FindContainerWithDirectChildren(root, "Prefix text", "Alpha inline-block", "suffix text");

        var childOrder = row.Children
            .Select(static child => ClassifyDirectChild(child, "Prefix text", "Alpha inline-block", "suffix text"))
            .Where(static label => label is not null)
            .Cast<string>()
            .ToList();

        childOrder.ShouldBe(["Prefix text", "Alpha inline-block", "suffix text"]);
    }

    [Fact]
    public async Task InlineBlock_SharedFormattingCase2_ShouldPlacePrefixAndSuffixBesideInlineBlock()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    Prefix text
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      Alpha inline-block
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>First block descendant</div>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>Second block descendant</div>
                    </span>
                    suffix text
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var row = FindContainerWithDirectChildren(root, "Prefix text", "Alpha inline-block", "suffix text");
        var prefix = row.Children.First(child => ContainsText(child, "Prefix text")).ShouldBeOfType<LineBoxFragment>();
        var inlineBlock = row.Children.First(child => ContainsText(child, "Alpha inline-block")).ShouldBeOfType<BlockFragment>();
        var suffix = row.Children.First(child => ContainsText(child, "suffix text")).ShouldBeOfType<LineBoxFragment>();
        var prefixRun = prefix.Runs.First(run => run.Text.Contains("Prefix text", StringComparison.OrdinalIgnoreCase));
        var suffixRun = suffix.Runs.First(run => run.Text.Contains("suffix text", StringComparison.OrdinalIgnoreCase));

        prefix.Rect.Y.ShouldBe(inlineBlock.Rect.Y, 0.1f);
        suffix.Rect.Y.ShouldBe(inlineBlock.Rect.Y, 0.1f);
        prefixRun.Origin.X.ShouldBeLessThan(inlineBlock.Rect.X);
        suffixRun.Origin.X.ShouldBeGreaterThanOrEqualTo(inlineBlock.Rect.Right - 0.1f);
    }

    [Fact]
    public async Task InlineBlock_SharedFormattingCase2_ShouldPreserveFullDescendantTextOrder()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    Prefix text
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      Alpha inline-block
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>First block descendant</div>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>
                        Third block descendant with
                        <span style='display: inline-block; margin-top: 4pt; padding: 4pt; border: 1pt solid #777;'>
                          nested inline-block
                          <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>Nested block descendant A</div>
                        </span>
                      </div>
                    </span>
                    suffix text
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];

        AssertIncreasingOrder(EnumerateOrderedText(root), [
            "Prefix text",
            "Alpha inline-block",
            "First block descendant",
            "Third block descendant with",
            "nested inline-block",
            "Nested block descendant A",
            "suffix"
        ]);
    }

    [Fact]
    public async Task InlineBlock_SharedFormattingCase2_ShouldPreserveInheritedFormattingAcrossSiblingsAndInlineBlockContent()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35; color: #222;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    Prefix text
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      Alpha inline-block
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>First block descendant</div>
                    </span>
                    suffix text
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var row = FindContainerWithDirectChildren(root, "Prefix text", "Alpha inline-block", "suffix text");
        var inlineBlock = row.Children.First(child => ContainsText(child, "Alpha inline-block")).ShouldBeOfType<BlockFragment>();

        var prefixRun = FindRun(row, "Prefix text");
        var alphaRun = FindRun(inlineBlock, "Alpha inline-block");
        var suffixRun = FindRun(row, "suffix text");
        var descendantRun = FindRun(inlineBlock, "First block descendant");

        prefixRun.Font.ShouldBe(alphaRun.Font);
        prefixRun.Font.ShouldBe(suffixRun.Font);
        prefixRun.Font.ShouldBe(descendantRun.Font);
        prefixRun.FontSizePt.ShouldBe(11f, 0.01f);
        alphaRun.FontSizePt.ShouldBe(prefixRun.FontSizePt, 0.01f);
        suffixRun.FontSizePt.ShouldBe(prefixRun.FontSizePt, 0.01f);
        descendantRun.FontSizePt.ShouldBe(prefixRun.FontSizePt, 0.01f);

        var alphaLine = FindLine(inlineBlock, "Alpha inline-block");
        var descendantLine = FindLine(inlineBlock, "First block descendant");

        alphaLine.LineHeight.ShouldBe(descendantLine.LineHeight, 0.01f);
    }

    [Fact]
    public async Task InlineBlock_SharedFormattingCase3_ShouldKeepTextBeforeInlineBlockAndTextAfterInSiblingOrder()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    text-before
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      outer-start
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-1</div>
                      <span style='display: inline-block; margin-top: 4pt; padding: 4pt; border: 1pt solid #777;'>
                        nested-inline-start
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-1</div>
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-2</div>
                      </span>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-2</div>
                      outer-end
                    </span>
                    text-after
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var row = FindContainerWithDirectChildren(root, "text-before", "outer-start", "text-after");

        var childOrder = row.Children
            .Select(static child => ClassifyDirectChild(child, "text-before", "outer-start", "text-after"))
            .Where(static label => label is not null)
            .Cast<string>()
            .ToList();

        childOrder.ShouldBe(["text-before", "outer-start", "text-after"]);
    }

    [Fact]
    public async Task InlineBlock_SharedFormattingCase3_ShouldPreserveDeepNestedTextOrder()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    text-before
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      outer-start
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-1</div>
                      <span style='display: inline-block; margin-top: 4pt; padding: 4pt; border: 1pt solid #777;'>
                        nested-inline-start
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-1</div>
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-2</div>
                      </span>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-2</div>
                      outer-end
                    </span>
                    text-after
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];

        EnumerateOrderedText(root).ShouldBe([
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

    [Fact]
    public async Task InlineBlock_SharedFormattingCase3_ShouldPlaceLeadingAndTrailingTextBesideInlineBlock()
    {
        const string html = @"
            <html>
              <body style='margin: 0; font-family: Arial, sans-serif; font-size: 11pt; line-height: 1.35;'>
                <div style='width: 320pt;'>
                  <div style='margin-bottom: 6pt;'>
                    text-before
                    <span style='display: inline-block; vertical-align: top; width: 190pt; border: 1pt solid #222; padding: 6pt; margin-right: 10pt;'>
                      outer-start
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-1</div>
                      <span style='display: inline-block; margin-top: 4pt; padding: 4pt; border: 1pt solid #777;'>
                        nested-inline-start
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-1</div>
                        <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>nested-block-2</div>
                      </span>
                      <div style='display: block; margin: 5pt 0; padding: 3pt 4pt; border: 1pt dashed #888;'>block-child-2</div>
                      outer-end
                    </span>
                    text-after
                  </div>
                </div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(6f));
        var root = (BlockFragment)layout.Pages[0].Children[0];
        var row = FindContainerWithDirectChildren(root, "text-before", "outer-start", "text-after");
        var leadingText = row.Children.First(child => ContainsText(child, "text-before")).ShouldBeOfType<LineBoxFragment>();
        var inlineBlock = row.Children.First(child => ContainsText(child, "outer-start")).ShouldBeOfType<BlockFragment>();
        var trailingText = row.Children.First(child => ContainsText(child, "text-after")).ShouldBeOfType<LineBoxFragment>();
        var leadingRun = leadingText.Runs.First(run => run.Text.Contains("text-before", StringComparison.OrdinalIgnoreCase));
        var trailingRun = trailingText.Runs.First(run => run.Text.Contains("text-after", StringComparison.OrdinalIgnoreCase));

        leadingText.Rect.Y.ShouldBe(inlineBlock.Rect.Y, 0.1f);
        trailingText.Rect.Y.ShouldBe(inlineBlock.Rect.Y, 0.1f);
        leadingRun.Origin.X.ShouldBeLessThan(inlineBlock.Rect.X);
        trailingRun.Origin.X.ShouldBeGreaterThanOrEqualTo(inlineBlock.Rect.Right - 0.1f);
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

    private static BlockFragment FindContainerWithDirectChildren(BlockFragment root, params string[] childTexts)
    {
        return EnumerateFragments(root)
            .OfType<BlockFragment>()
            .Where(block => CountMatchingDirectChildren(block, childTexts) >= childTexts.Length)
            .OrderBy(block => block.Children.Count)
            .First();
    }

    private static string? ClassifyDirectChild(LayoutFragment fragment, params string[] markers)
    {
        return markers.FirstOrDefault(marker => ContainsText(fragment, marker));
    }

    private static bool ContainsText(LayoutFragment fragment, string text)
    {
        return fragment switch
        {
            LineBoxFragment line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)),
            BlockFragment block => ContainsLineText(block, text),
            _ => false
        };
    }

    private static int CountMatchingDirectChildren(BlockFragment block, IReadOnlyList<string> markers)
    {
        return block.Children.Count(child => markers.Any(marker => ContainsText(child, marker)));
    }

    private static IReadOnlyList<string> EnumerateOrderedText(BlockFragment root)
    {
        var ordered = new List<string>();
        AppendOrderedText(root, ordered);
        return ordered;
    }

    private static TextRun FindRun(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .SelectMany(line => line.Runs)
            .First(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static LineBoxFragment FindLine(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<BlockFragment>()
            .SelectMany(block => block.Children.OfType<LineBoxFragment>())
            .First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }

    private static BlockFragment FindInlineBlock(BlockFragment fragment, string text)
    {
        return EnumerateFragments(fragment)
            .OfType<BlockFragment>()
            .First(block =>
                block.FormattingContext == FormattingContextKind.InlineBlock &&
                ContainsText(block, text));
    }

    private static void AppendOrderedText(LayoutFragment fragment, ICollection<string> ordered)
    {
        switch (fragment)
        {
            case LineBoxFragment line:
                var text = string.Concat(line.Runs.Select(run => run.Text)).Trim();
                if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "•", StringComparison.Ordinal))
                {
                    ordered.Add(text);
                }

                return;
            case BlockFragment block:
                foreach (var child in block.Children)
                {
                    AppendOrderedText(child, ordered);
                }

                return;
        }
    }

    private static void AssertIncreasingOrder(IReadOnlyList<string> texts, IReadOnlyList<string> expectedOrder)
    {
        var currentIndex = -1;
        foreach (var expected in expectedOrder)
        {
            var nextIndex = texts
                .Select((text, index) => (text, index))
                .First(pair => pair.index > currentIndex && pair.text.Contains(expected, StringComparison.OrdinalIgnoreCase))
                .index;

            nextIndex.ShouldBeGreaterThan(currentIndex);
            currentIndex = nextIndex;
        }
    }
}
