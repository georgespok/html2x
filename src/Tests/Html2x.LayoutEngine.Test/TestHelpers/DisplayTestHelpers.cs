using Html2x.RenderModel;
using Html2x.LayoutEngine.Style;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class DisplayTestHelpers
{
    private static readonly LayoutBuilderFixture Fixture = new();

    public static Task<HtmlLayout> BuildLayoutAsync(string html, float widthPerChar)
    {
        return Fixture.BuildLayoutAsync(html, CreateLinearMeasurer(widthPerChar), new LayoutBuildSettings
        {
            PageSize = PaperSizes.A4
        });
    }

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar)
    {
        return new FakeTextMeasurer(widthPerChar, 8f, 2f);
    }
}
