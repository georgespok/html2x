using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Moq;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class InlineFlowTestHelpers
{
    private static readonly LayoutBuilderFixture Fixture = new();

    public static Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer)
    {
        return Fixture.BuildLayoutAsync(html, textMeasurer, new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        });
    }

    public static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
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
}
