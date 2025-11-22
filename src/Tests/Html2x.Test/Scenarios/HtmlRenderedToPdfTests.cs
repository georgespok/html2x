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
        public async Task AnonymousBlockText_ShouldRenderWithInlineText()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                    <body>
                        <p>
                            Block Text
                        </p>
                        Anonymous Text
                    </body>
                </html>
                """;

            var converter = new HtmlConverter();
            var result = await converter.ToPdfAsync(html, DefaultOptions);

            await this.SavePdfForInspectionAsync(result.PdfBytes);

            var endLayoutBuild = result.Diagnostics!.Events.FirstOrDefault(x => x is { Type: DiagnosticsEventType.EndStage, Payload: not null, Name: "LayoutBuild" });

            var snapshot = ((LayoutSnapshotPayload) endLayoutBuild!.Payload!).Snapshot;

            Assert.NotNull(snapshot);

            var page = snapshot.Pages[0];
            page.Fragments[0].Children[0].Text.ShouldBe("Block Text");
            var blockTextX = page.Fragments[0].Children[0].X;
            page.Fragments[1].Children[0].Text.ShouldBe("Anonymous Text");
            var anonymousTextX = page.Fragments[1].Children[0].X;
            anonymousTextX.ShouldBe(blockTextX);
        }
    }
}
