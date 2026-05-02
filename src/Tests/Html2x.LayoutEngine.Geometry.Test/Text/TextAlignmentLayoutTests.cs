using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Moq;
using Shouldly;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.Text;

/// <summary>
/// Verifies text alignment affects run placement and line bounds.
/// </summary>
public class TextAlignmentLayoutTests
{
    private static readonly LayoutBuilderFixture Fixture = new();

    [Theory]
    [InlineData("center", "Center", 60f)]
    [InlineData("right", "Right", 125f)]
    public async Task Build_WhenParagraphUsesSupportedTextAlign_ShouldOffsetLineRuns(
        string textAlign,
        string text,
        float expectedOffset)
    {
        var html = $@"
            <html>
                <body style='margin: 0;'>
                    <div style='width: 200px; padding: 0; border: 0;'>
                        <p style='text-align: {textAlign};'>{text}</p>
                    </div>
                </body>
            </html>";

        var layout = await BuildLayoutAsync(html, CreateLinearMeasurer(5f));
        var line = FindLineByText(layout, text);

        line.Runs[0].Origin.X.ShouldBe(expectedOffset, 0.1f);
        AssertLineContainsRuns(line);
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
        AssertLineContainsRuns(line);
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
        return await Fixture.BuildLayoutAsync(html, textMeasurer, new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        });
    }

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.Measure(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey font, float _, string text) => TextMeasurement.CreateFallback(font, text.Length * widthPerChar, 8f, 2f));
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey _, float _, string text) => text.Length * widthPerChar);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((8f, 2f));
        return textMeasurer.Object;
    }

    private static void AssertLineContainsRuns(LineBoxFragment line)
    {
        foreach (var run in line.Runs)
        {
            line.Rect.Left.ShouldBeLessThanOrEqualTo(run.Origin.X + 0.1f);
            line.Rect.Right.ShouldBeGreaterThanOrEqualTo(run.Origin.X + run.AdvanceWidth - 0.1f);
        }
    }
}
