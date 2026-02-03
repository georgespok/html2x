using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;

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
}
