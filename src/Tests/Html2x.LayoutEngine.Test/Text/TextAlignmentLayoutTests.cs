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

public class TextAlignmentLayoutTests
{
    [Fact]
    public async Task CenterAlignedParagraph_OffsetsLineRuns()
    {
        const string html = @"
            <html>
                <body style='margin: 0;'>
                    <div style='width: 200px; padding: 0; border: 0;'>
                        <p style='text-align: center;'>Center</p>
                    </div>
                </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(5f));
        var line = FindLineByText(layout, "Center");

        var expectedContentWidth = 150f; // 200px -> 150pt
        var expectedLineWidth = 6 * 5f;
        var expectedOffset = (expectedContentWidth - expectedLineWidth) / 2f;

        line.Runs[0].Origin.X.ShouldBe(expectedOffset, 0.1f);
    }

    [Fact]
    public async Task RightAlignedParagraph_OffsetsLineRuns()
    {
        const string html = @"
            <html>
                <body style='margin: 0;'>
                    <div style='width: 200px; padding: 0; border: 0;'>
                        <p style='text-align: right;'>Right</p>
                    </div>
                </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(5f));
        var line = FindLineByText(layout, "Right");

        var expectedContentWidth = 150f; // 200px -> 150pt
        var expectedLineWidth = 5 * 5f;
        var expectedOffset = expectedContentWidth - expectedLineWidth;

        line.Runs[0].Origin.X.ShouldBe(expectedOffset, 0.1f);
    }

    [Fact]
    public async Task JustifiedParagraph_DistributesSpaceAcrossLine()
    {
        const string html = @"
            <html>
                <body style='margin: 0;'>
                    <div style='width: 140px; padding: 0; border: 0;'>
                        <p style='text-align: justify;'>Hello world again more</p>
                    </div>
                </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(5f));
        var line = FindLineByText(layout, "Hello world again");

        var expectedContentWidth = 105f; // 140px -> 105pt
        var lastRun = line.Runs[^1];
        var lastRight = lastRun.Origin.X + lastRun.AdvanceWidth;

        lastRight.ShouldBe(expectedContentWidth, 0.5f);
    }

    private static LineBoxFragment FindLineByText(HtmlLayout layout, string text)
    {
        foreach (var page in layout.Pages)
        {
            foreach (var block in page.Children.OfType<BlockFragment>())
            {
                var line = FindLineByText(block, text);
                if (line is not null)
                {
                    return line;
                }
            }
        }

        throw new InvalidOperationException($"Line containing '{text}' not found.");
    }

    private static LineBoxFragment? FindLineByText(BlockFragment block, string text)
    {
        foreach (var child in block.Children)
        {
            if (child is LineBoxFragment line)
            {
                var lineText = string.Concat(line.Runs.Select(r => r.Text));
                if (string.Equals(lineText, text, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            if (child is BlockFragment nested)
            {
                var nestedLine = FindLineByText(nested, text);
                if (nestedLine is not null)
                {
                    return nestedLine;
                }
            }
        }

        return null;
    }

    private static async Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer)
    {
        var config = Configuration.Default.WithCss();
        var domProvider = new AngleSharpDomProvider(config);
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new CssValueConverter());
        var boxBuilder = new BoxTreeBuilder(textMeasurer);
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
