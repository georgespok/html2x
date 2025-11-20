using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Runtime;
using Html2x.Diagnostics.Sinks;
using Html2x.Renderers.Pdf.Options;

namespace Html2x.Test.Scenarios;

public sealed class BasicHtmlDiagnosticsTests
{
    private static PdfOptions DefaultOptions => new()
    {
        FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
    };

    [Fact]
    public async Task ParagraphLineBreaks_ShouldEmitLineMetadata()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
                <body>
                    <p style="color: #336699; text-align: right;">
                        first line<br/>second line
                    </p>
                </body>
            </html>
            """;

        InMemoryDiagnosticSink? sink = null;
        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddInMemorySink("scenario-diagnostics", configure: s => sink = s);
        });

        var converter = runtime.Decorate(new HtmlConverter());

        using var session = runtime.StartSession("basic-html");
        await converter.ToPdfAsync(html, DefaultOptions);

        var snapshot = AssertDiagnosticsSnapshot(sink);
        var textFragments = snapshot.Fragments
            .Where(f => string.Equals(f.Type, "text", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.LineIndex)
            .ToArray();

        Assert.Equal(2, textFragments.Length);

        Assert.Equal(0, textFragments[0].LineIndex);
        Assert.Equal("#336699", textFragments[0].Color);
        Assert.Equal("right", textFragments[0].TextAlign);
        Assert.True(textFragments[0].LineHeight > 0);

        Assert.Equal(1, textFragments[1].LineIndex);
        Assert.Equal("#336699", textFragments[1].Color);
        Assert.Equal("right", textFragments[1].TextAlign);
        Assert.True(textFragments[1].LineHeight > 0);
    }

    private static DiagnosticsSnapshot AssertDiagnosticsSnapshot(InMemoryDiagnosticSink? sink)
    {
        Assert.NotNull(sink);

        var entry = Assert.Single(
            sink!.Entries,
            model => model.Event.Kind == "diagnostics/fragments/snapshot");

        Assert.True(entry.Event.Payload.TryGetValue("snapshot", out var payload));
        return Assert.IsType<DiagnosticsSnapshot>(payload);
    }
}
