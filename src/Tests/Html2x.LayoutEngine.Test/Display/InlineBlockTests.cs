using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Test.TestHelpers;
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
}
