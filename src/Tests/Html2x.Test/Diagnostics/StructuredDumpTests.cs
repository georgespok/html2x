using System.Text.Json;
using AngleSharp;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Runtime;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Dom;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Style;
using Html2x.Renderers.Pdf.Options;

namespace Html2x.Test.Diagnostics;

public sealed class StructuredDumpTests
{
    private const string LayoutDumpCategory = "dump/layout";

    private const string SampleHtml = """
        <!DOCTYPE html>
        <html>
            <body>
                <section id="article">
                    <header id="article-header">
                        <h1>Structured dump sample</h1>
                    </header>
                    <article id="article-body">
                        <p data-node="lead">First paragraph.</p>
                        <p data-node="detail"><strong>Second</strong> paragraph.</p>
                    </article>
                    <footer id="article-footer">
                        <p>Footer copy</p>
                    </footer>
                </section>
            </body>
        </html>
        """;

    private static PdfOptions DefaultOptions => new()
    {
        FontPath = Path.Combine("Fonts", "Inter-Regular.ttf")
    };

    [Fact]
    public async Task LayoutDump_ShouldExposeStableNodeCountAndIdentifiers()
    {
        var sink = new InMemorySink();
        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddSink("memory", "structured-dump", () => sink);
        });

        var firstDump = await CaptureLayoutDumpAsync(runtime, sink, "layout-dump-first");
        var secondDump = await CaptureLayoutDumpAsync(runtime, sink, "layout-dump-second");

        Assert.NotNull(firstDump);
        Assert.NotNull(secondDump);
        Assert.True(firstDump!.NodeCount > 0);
        Assert.Equal(firstDump.NodeCount, secondDump!.NodeCount);

        var firstIdentifiers = ExtractNodeIdentifiers(firstDump.Body);
        var secondIdentifiers = ExtractNodeIdentifiers(secondDump.Body);

        Assert.Equal(firstIdentifiers, secondIdentifiers);
    }

    private static async Task<StructuredDumpMetadata> CaptureLayoutDumpAsync(
        IDiagnosticsRuntime runtime,
        InMemorySink sink,
        string sessionName)
    {
        sink.Reset();
        using var session = runtime.StartSession(sessionName);
        var domProvider = new AngleSharpDomProvider(Configuration.Default.WithCss());
        var styleComputer = new CssStyleComputer(new StyleTraversal(), new UserAgentDefaults());
        var boxBuilder = new BoxTreeBuilder();
        var fragmentBuilder = new FragmentBuilder();
        var layoutBuilder = new LayoutBuilder(domProvider, styleComputer, boxBuilder, fragmentBuilder, session);
        await layoutBuilder.BuildAsync(SampleHtml, DefaultOptions.PageSize);

        var dumpEvent = sink.Events
            .Select(model => model.Event)
            .FirstOrDefault(evt => evt.Category == LayoutDumpCategory && evt.Dump is not null);

        Assert.NotNull(dumpEvent);
        Assert.NotNull(dumpEvent!.Dump);

        return dumpEvent.Dump!;
    }

    private static IReadOnlyList<string> ExtractNodeIdentifiers(string body)
    {
        using var document = JsonDocument.Parse(body);
        Assert.True(
            document.RootElement.TryGetProperty("nodes", out var nodesElement),
            "Layout dump must provide a nodes array.");

        var identifiers = new List<string>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            if (!nodeElement.TryGetProperty("id", out var idProperty))
            {
                continue;
            }

            var value = idProperty.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                identifiers.Add(value);
            }
        }

        Assert.NotEmpty(identifiers);
        return identifiers;
    }

    private sealed class InMemorySink : IDiagnosticSink
    {
        public string SinkId => "structured-dump-sink";

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
