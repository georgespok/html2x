using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Abstractions.Options;
using Moq;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class InlineFlowTestHelpers
{
    private static readonly LayoutBuilderFixture Fixture = new();

    public static Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer)
    {
        return Fixture.BuildLayoutAsync(html, textMeasurer, new LayoutOptions
        {
            PageSize = PaperSizes.A4
        });
    }

    public static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns((FontKey _, float _, string text) => text.Length * widthPerChar);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((8f, 2f));
        return textMeasurer.Object;
    }
}
