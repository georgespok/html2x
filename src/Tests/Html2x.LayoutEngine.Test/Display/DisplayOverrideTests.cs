using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Display;

public class DisplayOverrideTests
{
    [Fact]
    public async Task DisplayOverride_BlockOnSpan_CreatesBlockFragment()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <span style='display: block; height: 12pt;'>BlockSpan</span>
              </body>
            </html>";

        var layout = await DisplayTestHelpers.BuildLayoutAsync(html, 10f);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);
        layout.Pages[0].Children[0].ShouldBeOfType<BlockFragment>();
    }

}
