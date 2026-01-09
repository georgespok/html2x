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
    public async Task ParagraphWithLongText_ComputesHeightFromMultipleLines()
    {
        const string html = "<html><body><div style=\"max-width: 80px; font-size: 10px; line-height: 1.4;\">alpha beta gamma delta</div></body></html>";
        
        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var block = (BlockFragment)layout.Pages[0].Children[0];
        var lineBoxes = block.Children.OfType<LineBoxFragment>().ToList();

        lineBoxes.Count.ShouldBeGreaterThan(1);
        block.Rect.Height.ShouldBeGreaterThan(lineBoxes[0].LineHeight * 1.5f);
    }

    [Fact]
    public async Task ParagraphWithBrProducesDistinctLineBoxes()
    {
        const string html = "<html><body><p>first line<br/>second line</p></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

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

    [Fact]
    public async Task ParagraphWithSpan_EmitsSpanTextAsRun()
    {
        const string html = "<html><body><p>alpha <span>beta</span> gamma</p></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var paragraph = (BlockFragment)layout.Pages[0].Children[0];
        var line = paragraph.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        line.Runs.Count.ShouldBe(3);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("alpha beta gamma");
    }

    [Fact]
    public async Task ParagraphWithUnderlineStrikethroughAndSpan_EmitsAllTextRuns()
    {
        const string html = "<html><body><p>This is <u>underlined</u> text <s>struck</s> and <span>spanned</span>.</p></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var paragraph = (BlockFragment)layout.Pages[0].Children[0];
        var line = paragraph.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        line.Runs.Count.ShouldBeGreaterThan(4);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("This is underlined text struck and spanned.");
    }

    [Fact]
    public async Task ParagraphWithUnsupportedInline_DoesNotDropFollowingRuns()
    {
        const string html = "<html><body><p>alpha <blink>beta</blink> gamma</p></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var paragraph = (BlockFragment)layout.Pages[0].Children[0];
        var line = paragraph.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("alpha beta gamma");
    }

    [Fact]
    public async Task FontFamilyList_UsesFirstFamilyToken()
    {
        const string html = "<html><body><p style=\"font-family: 'Inter', Arial, sans-serif\">text</p></body></html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(10f));

        var paragraph = (BlockFragment)layout.Pages[0].Children[0];
        var line = paragraph.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        line.Runs.Count.ShouldBe(1);
        line.Runs[0].Font.Family.ShouldBe("Inter");
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
            .Returns((8f, 2f));
        return textMeasurer.Object;
    }
}
