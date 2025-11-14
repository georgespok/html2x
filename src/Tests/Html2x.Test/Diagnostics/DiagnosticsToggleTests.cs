using Html2x.Diagnostics.Runtime;
using Html2x.Renderers.Pdf.Options;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Test.Diagnostics;

public sealed class DiagnosticsToggleTests
{
    private const string SampleHtml = """
        <!DOCTYPE html>
        <html>
            <body>
                <p>Diagnostics toggle smoke test.</p>
            </body>
        </html>
        """;

    private static PdfOptions DefaultOptions => new()
    {
        FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
    };

    [Fact]
    public async Task DiagnosticsDisabledByDefault_EmitsNoEvents()
    {
        var sink = new InMemorySink();
        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddSink("memory", "test", () => sink);
        });

        var converter = runtime.Decorate(new HtmlConverter());

        // Disabled run: diagnostics session never created.
        sink.Reset();
        await converter.ToPdfAsync(SampleHtml, DefaultOptions);
        Assert.Empty(sink.Events);

        // Enabled run: diagnostics session should emit stage events.
        sink.Reset();
        using var session = runtime.StartSession("diagnostics-enabled");
        await converter.ToPdfAsync(SampleHtml, DefaultOptions);
        Assert.NotEmpty(sink.Events); 
    }

    private sealed class InMemorySink : IDiagnosticSink
    {
        public string SinkId => "in-memory-test";

        public List<DiagnosticsModel> Events { get; } = [];

        public void Publish(DiagnosticsModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Events.Add(model);
        }

        public void Reset() => Events.Clear();
    }
}
