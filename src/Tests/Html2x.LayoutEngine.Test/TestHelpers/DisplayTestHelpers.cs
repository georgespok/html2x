using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Measurements.Units;
using Html2x.Text;

namespace Html2x.LayoutEngine.Test.TestHelpers;

internal static class DisplayTestHelpers
{
    private static readonly LayoutBuilderFixture Fixture = new();

    public static Task<HtmlLayout> BuildLayoutAsync(string html, float widthPerChar) =>
        Fixture.BuildLayoutAsync(html, CreateLinearMeasurer(widthPerChar), new()
        {
            PageSize = PaperSizes.A4
        });

    private static ITextMeasurer CreateLinearMeasurer(float widthPerChar) => new FakeTextMeasurer(widthPerChar, 8f, 2f);
}