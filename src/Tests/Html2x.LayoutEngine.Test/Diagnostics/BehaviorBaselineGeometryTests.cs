using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.RenderModel.Fragments;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

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

    [Fact]
    public async Task Build_BlockTableImageAndInlineFlow_PreservesPublishedFragmentFacts()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0; padding: 4px; border: 1px solid black;'>
                  Alpha <span>Beta</span>
                  <img src='image.png' width='40' height='20' style='display: block;' />
                  <table style='margin: 0; width: 120px;'>
                    <tr>
                      <th>A</th>
                      <td>B</td>
                    </tr>
                  </table>
                </div>
              </body>
            </html>
            """);

        var fragments = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .ToList();

        fragments.OfType<LineBoxFragment>()
            .ShouldContain(static line =>
                line.Runs.Any(static run => run.Text.Contains("Alpha", StringComparison.Ordinal)));

        var image = fragments.OfType<ImageFragment>().ShouldHaveSingleItem();
        image.ContentRect.Width.ShouldBe(30f, 0.01f);
        image.ContentRect.Height.ShouldBe(15f, 0.01f);

        var table = fragments.OfType<TableFragment>().ShouldHaveSingleItem();
        table.DerivedColumnCount.ShouldBe(2);

        var header = fragments.OfType<TableCellFragment>()
            .Single(static cell => cell.IsHeader);
        header.ColumnIndex.ShouldBe(0);
    }

    [Fact]
    public async Task Build_UnsupportedTable_PublishesPlaceholderAndDiagnostics()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <table style='margin: 0; width: 120px;'>
                  <tr>
                    <td colspan='2'>A</td>
                  </tr>
                </table>
              </body>
            </html>
            """);

        var table = result.PublishedLayout.Blocks.ShouldHaveSingleItem();
        table.Geometry.Height.ShouldBe(0f);
        table.Children.ShouldBeEmpty();

        result.Diagnostics
            .ShouldContain(record =>
                record.Name == "layout/table/unsupported-structure" &&
                record.Severity == DiagnosticSeverity.Error);
        result.Diagnostics
            .ShouldContain(record =>
                record.Name == "layout/table" &&
                record.Fields["outcome"] == new DiagnosticStringValue("Unsupported"));
    }

    private static LineBoxFragment FindLine(IEnumerable<LineBoxFragment> lines, string text)
    {
        return lines.First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<LineBoxFragment> EnumerateLines(LayoutFragment fragment) =>
        EnumerateFragments(fragment).OfType<LineBoxFragment>();

    private static IEnumerable<LineBoxFragment> EnumerateLines(IEnumerable<LayoutFragment> fragments) =>
        fragments.SelectMany(EnumerateFragments).OfType<LineBoxFragment>();

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