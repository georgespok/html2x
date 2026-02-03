using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Options;
using Html2x.Diagnostics;
using Shouldly;
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
        public async Task BoldItalicUnderlineText_ShouldRenderProperly()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                    This is <b>bold</b> text. This is <i>italic</i> text. 
                    This is <u>underlined</u> text
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);

            await SavePdfForInspectionAsync(result.PdfBytes);

            var page = GetLayoutPageSnapshot(result);

            Assert.NotNull(page);

            page.Fragments.ShouldNotBeEmpty();
        }

        private static LayoutPageSnapshot GetLayoutPageSnapshot(Html2PdfResult result)
        {
            var endLayoutBuild = result.Diagnostics!.Events.FirstOrDefault(x => x is { Type: DiagnosticsEventType.EndStage, Payload: not null, Name: "LayoutBuild" });

            var snapshot = ((LayoutSnapshotPayload) endLayoutBuild!.Payload!).Snapshot;

            return snapshot.Pages[0];
        }
    }
}
