using AngleSharp;
using Html2x.Abstractions.Images;
using Html2x.Abstractions.Layout.Documents;
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

    [Fact]
    public async Task ParagraphWithSpaces_WrapsAtWhitespaceWhenPossible()
    {
        const string html = "<html><body><div style=\"max-width: 80px\">alpha beta gamma</div></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lineBoxes = block.Children.OfType<LineBoxFragment>().ToList();

        lineBoxes.Count.ShouldBeGreaterThan(1);
        lineBoxes[0].Runs.Single().Text.ShouldBe("alpha");
        lineBoxes[1].Runs.Single().Text.ShouldBe("beta");
    }

    [Fact]
    public async Task LongToken_WrapsByGraphemeFallback()
    {
        const string html = "<html><body><div style=\"max-width: 40px\">Supercalifragilisticexpialidocious</div></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lineBoxes = block.Children.OfType<LineBoxFragment>().ToList();

        lineBoxes.Count.ShouldBeGreaterThan(1);
        lineBoxes.All(line => line.Runs.Count == 1).ShouldBeTrue();
    }

    private static async Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer)
    {
        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var imageProvider = new NoopImageProvider();
        var layoutBuilder = CreateLayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider, textMeasurer);
        var options = new LayoutOptions
        {
            PageSize = PaperSizes.A4
        };

        return await layoutBuilder.BuildAsync(html, options);
    }

    private static LayoutBuilder CreateLayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider)
    {
        return CreateLayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider, CreateLinearMeasurer(0f));
    }

    private static LayoutBuilder CreateLayoutBuilder(
        IDomProvider domProvider,
        IStyleComputer styleComputer,
        IBoxTreeBuilder boxBuilder,
        IFragmentBuilder fragmentBuilder,
        IImageProvider imageProvider,
        ITextMeasurer textMeasurer)
    {
        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, imageProvider, textMeasurer, fontSource.Object);
    }

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey _, float _, string text) => text.Length * widthPerChar);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((0f, 0f));
        return textMeasurer.Object;
    }
}
