using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Display;

public class ListItemTests
{
    [Fact]
    public async Task ListItem_ReservesMarkerSpace()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <ul>
                  <li>First</li>
                  <li>Second</li>
                </ul>
              </body>
            </html>";

        var layout = await DisplayTestHelpers.BuildLayoutAsync(html, 10f);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var list = (BlockFragment)layout.Pages[0].Children[0];
        list.Children.Count.ShouldBe(2);

        var item1 = list.Children[0].ShouldBeOfType<BlockFragment>();
        var item2 = list.Children[1].ShouldBeOfType<BlockFragment>();

        var item1Line = item1.Children.OfType<LineBoxFragment>().First();
        var item2Line = item2.Children.OfType<LineBoxFragment>().First();

        item1Line.Runs.Count.ShouldBeGreaterThanOrEqualTo(2);
        item2Line.Runs.Count.ShouldBeGreaterThanOrEqualTo(2);

        item1Line.Runs[0].Text.ShouldContain("•");
        item2Line.Runs[0].Text.ShouldContain("•");

        var expectedOffset = HtmlCssConstants.Defaults.ListMarkerOffsetPt;
        item1Line.Rect.X.ShouldBeGreaterThanOrEqualTo(expectedOffset - 0.5f);
        item2Line.Rect.X.ShouldBeGreaterThanOrEqualTo(expectedOffset - 0.5f);
    }

    [Fact]
    public async Task ListItem_InsideInlineBlock_PreservesMarkerSemantics()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                  <span style='display: inline-block; border: 1pt solid #000; padding: 2pt;'>
                    <ul>
                      <li>First item</li>
                      <li>Second item</li>
                    </ul>
                  </span>
                </div>
              </body>
            </html>";

        var layout = await DisplayTestHelpers.BuildLayoutAsync(html, 10f);

        var root = (BlockFragment)layout.Pages[0].Children[0];
        var inlineBlock = EnumerateFragments(root)
            .OfType<BlockFragment>()
            .FirstOrDefault(fragment => fragment.Style?.Borders?.HasAny == true);
        inlineBlock.ShouldNotBeNull();

        var lineRuns = EnumerateFragments(inlineBlock)
            .OfType<LineBoxFragment>()
            .SelectMany(line => line.Runs)
            .Select(run => run.Text)
            .ToList();

        lineRuns.Count(text => text.Contains("•", StringComparison.Ordinal))
            .ShouldBeGreaterThanOrEqualTo(2);
        lineRuns.ShouldContain(text => text.Contains("First item", StringComparison.Ordinal));
        lineRuns.ShouldContain(text => text.Contains("Second item", StringComparison.Ordinal));
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
