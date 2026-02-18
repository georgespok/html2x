using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Display;

public class InlineFlowTests
{
    [Fact]
    public async Task InlineFlow_WrapsTextIntoLineBoxes()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 40pt;'>AAAA BBBB</div>
              </body>
            </html>";

        var layout = await InlineFlowTestHelpers.BuildLayoutAsync(html, InlineFlowTestHelpers.CreateLinearMeasurer(10f));

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lines = block.Children.OfType<LineBoxFragment>().ToList();

        lines.Count.ShouldBe(2);
        lines[0].Runs.Count.ShouldBe(1);
        lines[1].Runs.Count.ShouldBe(1);

        lines[0].Runs[0].Text.ShouldBe("AAAA");
        lines[1].Runs[0].Text.ShouldBe("BBBB");

        lines[0].Rect.Y.ShouldBeLessThan(lines[1].Rect.Y);
    }
}
