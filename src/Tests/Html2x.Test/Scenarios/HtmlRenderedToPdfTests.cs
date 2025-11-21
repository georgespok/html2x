using Html2x.Abstractions.Options;
using Xunit.Abstractions;


namespace Html2x.Test.Scenarios
{
    public class HtmlRenderedToPdfTests(ITestOutputHelper output) : IntegrationTestBase(output)
    {

        public HtmlConverterOptions DefaultOptions => new()
        {
            Diagnostics = new DiagnosticsOptions
            {
                EnableDiagnostics = true
            },
            Pdf = new PdfOptions
            {
                FontPath = Path.Combine("Fonts", "Inter-Regular.ttf"),
                EnableDebugging = true
            }
        };

        [Fact]
        public async Task ListItem_ShouldRenderWithoutErrors()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                    <body>
                        <p>
                        First line<br />Second
                        line
                        </p>
                    </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);

            await this.SavePdfForInspectionAsync(result.PdfBytes);
        }
    }
}
