using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Runtime;
using Html2x.Renderers.Pdf.Options;

namespace Html2x.Test.Diagnostics;

public sealed class DiagnosticsSessionEnvelopeTests
{
    private const string SampleHtml = """
        <!DOCTYPE html>
        <html>
            <body>
                <p>Diagnostics envelope smoke test.</p>
            </body>
        </html>
        """;

    private static PdfOptions DefaultOptions => new()
    {
        FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
    };

    [Fact]
    public async Task ToPdfAsync_SingleRun_CreatesOneEnvelope()
    {
        var sink = new InMemorySink();

        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddSink("memory", "envelope-test", () => sink);
        });

        var converter = runtime.Decorate(new HtmlConverter());

        using var session = runtime.StartSession("single-envelope");
        await converter.ToPdfAsync(SampleHtml, DefaultOptions);

        var envelopeEvents = sink.Events
            .Where(model => model.Event.Category == "session" && model.Event.Kind == "session/envelope")
            .ToArray();

        Assert.Single(envelopeEvents);

        var sessionIds = sink.Events.Select(model => model.Event.SessionId).Distinct().ToArray();
        Assert.Single(sessionIds);
        Assert.Equal(sessionIds[0], envelopeEvents[0].Event.SessionId);
    }

    private sealed class InMemorySink : IDiagnosticSink
    {
        public string SinkId => "single-envelope-memory";

        public List<DiagnosticsModel> Events { get; } = [];

        public void Publish(DiagnosticsModel model)
        {
            Events.Add(model);
        }
    }
}
