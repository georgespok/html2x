using AngleSharp;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public class LineBoxFragmentTests
{
    [Fact]
    public async Task ParagraphWithBrProducesDistinctLineBoxes()
    {
        const string html = "<html><body><p>first line<br/>second line</p></body></html>";

        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var imageProvider = new NoopImageProvider();
        var layoutBuilder = new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider);
        var options = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        var layout = await layoutBuilder.BuildAsync(html, options);

        layout.Pages.Count.ShouldBe(1);
        layout.Pages[0].Children.Count.ShouldBe(1);

        var paragraph = (BlockFragment)layout.Pages[0].Children[0];
        var lineBoxes = paragraph.Children.OfType<LineBoxFragment>().ToList();
        lineBoxes.Count.ShouldBe(2);
        lineBoxes[0].Runs.Single().Text.ShouldBe("first line");
        lineBoxes[1].Runs.Single().Text.ShouldBe("second line");
    }
}
