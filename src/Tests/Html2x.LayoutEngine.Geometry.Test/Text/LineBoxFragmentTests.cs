using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Geometry.Test.Text;

public class LineBoxFragmentTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

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
    public async Task ParagraphWithSpaces_WrapsAtWhitespace()
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
        return await Fixture.BuildLayoutAsync(html, textMeasurer, new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        });
    }

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        return new FakeTextMeasurer(widthPerChar, 8f, 2f);
    }
}
