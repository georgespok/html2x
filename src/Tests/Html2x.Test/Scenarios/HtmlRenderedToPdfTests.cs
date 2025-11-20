using Html2x.Renderers.Pdf.Options;
using Xunit.Abstractions;


namespace Html2x.Test.Scenarios
{
    public class HtmlRenderedToPdfTests(ITestOutputHelper output) : IntegrationTestBase(output)
    {

        public PdfOptions DefaultOptions => new()
        {
            FontPath = Path.Combine("Fonts", "Inter-Regular.ttf"),
            EnableDebugging = true
        };

        [Fact]
        public async Task ListItem_ShouldRenderWithoutErrors()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                    <body>
                        <ul>
                            <li>Item 1</li>
                            <li>Item 2</li>
                        </ul>
                    </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var bytes = await converter.ToPdfAsync(html, DefaultOptions);

            await this.SavePdfForInspectionAsync(bytes);
        }
    }
}
