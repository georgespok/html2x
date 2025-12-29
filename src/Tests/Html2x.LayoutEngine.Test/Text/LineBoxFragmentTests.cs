using AngleSharp;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Moq;
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
        var layoutBuilder = CreateLayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider);
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

    private static LayoutBuilder CreateLayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(0f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((0f, 0f));

        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider, textMeasurer.Object, fontSource.Object);
    }
}
