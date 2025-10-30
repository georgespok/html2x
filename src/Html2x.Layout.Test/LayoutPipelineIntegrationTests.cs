using AngleSharp;
using Html2x.Core;
using Html2x.Core.Layout;
using Html2x.Layout;
using Html2x.Layout.Box;
using Html2x.Layout.Dom;
using Html2x.Layout.Fragment;
using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test;

public class LayoutPipelineIntegrationTests
{
    [Fact]
    public async Task BodyMarginTop_IsAppliedToFirstBlockFragmentPosition()
    {
        // Arrange: 96px body margin-top (-> 72pt) + 12pt paragraph margin-top => expected first block Y = 84pt
        const string html = @"
            <html>
              <body style='margin: 96px;'>
                <p style='margin-top: 12pt;'>Paragraph</p>
                <div>DIV</div>
              </body>
            </html>";

        var config = Configuration.Default.WithCss();
        var dom = new AngleSharpDomProvider(config);
        var style = new CssStyleComputer();
        var boxes = new BoxTreeBuilder();
        var fragments = new FragmentBuilder();
        var builder = new LayoutBuilder(dom, style, boxes, fragments);

        // Act
        var layout = await builder.BuildAsync(html, PaperSizes.A4);

        // Assert
        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(2);
        
        var p = (BlockFragment) layout.Pages[0].Children[0];
        p.Rect.Y.ShouldBe(84f);
        p.Children.Count.ShouldBe(1);
        var div = layout.Pages[0].Children[1];
        div.Rect.Y.ShouldBe(112f, 0.5f);
        
    }
}


