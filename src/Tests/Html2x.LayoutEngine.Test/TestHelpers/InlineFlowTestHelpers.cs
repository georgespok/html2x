using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class InlineFlowTestHelpers
{
    private static readonly LayoutBuilderFixture Fixture = new();

    public static Task<HtmlLayout> BuildLayoutAsync(string html, ITextMeasurer textMeasurer) =>
        Fixture.BuildLayoutAsync(html, textMeasurer, new()
        {
            PageSize = PaperSizes.A4
        });

    public static ITextMeasurer CreateLinearMeasurer(float widthPerChar) => new FakeTextMeasurer(widthPerChar, 8f, 2f);
}