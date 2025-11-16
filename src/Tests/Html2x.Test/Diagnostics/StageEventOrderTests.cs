using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Runtime;
using Html2x.Renderers.Pdf.Options;

namespace Html2x.Test.Diagnostics;

public sealed class StageEventOrderTests
{
    private static readonly string[] ExpectedStages =
    [
        "stage/style",
        "stage/layout",
        "stage/inline-measurement",
        "stage/fragmentation",
        "stage/pagination",
        "stage/pdf-render"
    ];

    private const string SampleHtml = """
        <!DOCTYPE html>
        <html>
            <body>
                <p>Diagnostics stage order smoke test.</p>
            </body>
        </html>
        """;

    private static PdfOptions DefaultOptions => new()
    {
        FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
    };

    [Fact]
    public async Task StageEvents_ShouldEmitStartStopForEachStage_InOrder()
    {
        var sink = new InMemorySink();
        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddSink("memory", "test", () => sink);
        });

        var converter = runtime.Decorate(new HtmlConverter());

        using var session = runtime.StartSession("stage-order");
        await converter.ToPdfAsync(SampleHtml, DefaultOptions);

        var categories = sink.Events
            .Select(e => e.Event.Category)
            .Where(category => category.StartsWith("stage/", StringComparison.Ordinal))
            .ToArray();

        var expectedSequence = ExpectedStages
            .SelectMany(stage => new[] { stage, stage })
            .ToArray(); // start + stop per stage

        Assert.Equal(expectedSequence, categories); 
    }

    private sealed class InMemorySink : IDiagnosticSink
    {
        public string SinkId => "stage-order-memory";

        public List<DiagnosticsModel> Events { get; } = [];

        public void Publish(DiagnosticsModel model)
        {
            Events.Add(model);
        }
    }
}
