using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Measurements.Units;
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
        return new Html2x.LayoutEngine.Test.TestDoubles.FakeTextMeasurer(widthPerChar, 8f, 2f);
    }
}
