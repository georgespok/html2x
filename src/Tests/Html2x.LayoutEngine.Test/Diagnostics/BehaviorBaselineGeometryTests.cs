using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class BehaviorBaselineGeometryTests
{
    [Fact]
    public async Task Build_NarrowInlineText_BaselineWrapsIntoMultipleLines()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; width: 20px;'>Alpha Beta</div>
              </body>
            </html>
            """);

        var lines = EnumerateLines(result.Layout.Pages.SelectMany(static page => page.Children)).ToList();

        lines.Count.ShouldBeGreaterThanOrEqualTo(2);
        lines[1].Rect.Y.ShouldBeGreaterThan(lines[0].Rect.Y);
    }

    [Fact]
    public async Task Build_InlineBlockNestedBlocks_PreservesLineOrderAndHeight()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  <span style='display: inline-block; padding: 2pt; border: 1pt solid black;'>
                    inline-before
                    <div style='margin: 0;'>block-one</div>
                    inline-after
                  </span>
                </div>
              </body>
            </html>
            """);

        var inlineBlock = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .OfType<BlockFragment>()
            .First(static fragment => fragment.FormattingContext == FormattingContextKind.InlineBlock);
        var lines = EnumerateLines(inlineBlock).ToList();

        FindLine(lines, "block-one").Rect.Y.ShouldBeGreaterThan(FindLine(lines, "inline-before").Rect.Y);
        FindLine(lines, "inline-after").Rect.Y.ShouldBeGreaterThan(FindLine(lines, "block-one").Rect.Y);
        inlineBlock.Rect.Bottom.ShouldBeGreaterThanOrEqualTo(FindLine(lines, "inline-after").Rect.Bottom - 0.01f);
    }

    [Fact]
    public async Task Build_RuleFragment_BaselineEmitsRuleInsideBlockFlow()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <hr style='display: block; margin: 0; border-top-width: 2pt; border-top-style: solid;' />
                  after
                </div>
              </body>
            </html>
            """);

        var fragments = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .ToList();
        var rule = fragments.OfType<RuleFragment>().FirstOrDefault();
        var before = FindLine(fragments.OfType<LineBoxFragment>(), "before");
        var after = FindLine(fragments.OfType<LineBoxFragment>(), "after");

        rule.ShouldNotBeNull();
        rule.Rect.Y.ShouldBeGreaterThanOrEqualTo(before.Rect.Bottom - 0.01f);
        after.Rect.Y.ShouldBeGreaterThanOrEqualTo(rule.Rect.Bottom - 0.01f);
    }

    private static LineBoxFragment FindLine(IEnumerable<LineBoxFragment> lines, string text)
    {
        return lines.First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<LineBoxFragment> EnumerateLines(LayoutFragment fragment)
    {
        return EnumerateFragments(fragment).OfType<LineBoxFragment>();
    }

    private static IEnumerable<LineBoxFragment> EnumerateLines(IEnumerable<LayoutFragment> fragments)
    {
        return fragments.SelectMany(EnumerateFragments).OfType<LineBoxFragment>();
    }

    private static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateFragments(child))
            {
                yield return nested;
            }
        }
    }
}
