using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Runtime;

namespace Html2x.Test.Diagnostics;

public sealed class DiagnosticContextTests
{
    private const string ContextCategory = "context/detail";

    [Fact]
    public void DisposedContext_ShouldEmitContextDetailEventWithCapturedValues()
    {
        var sink = new InMemorySink();
        var runtime = DiagnosticsRuntime.Configure(builder =>
        {
            builder.AddSink("memory", "context", () => sink);
        });

        using var session = runtime.StartSession("context-test");
        var context = session.Context("ShrinkToFit");

        context.Set("availableWidth", 140);
        context.Set("intrinsicWidth", 210);

        Assert.DoesNotContain(
            sink.Events,
            model => model.Event.Category == ContextCategory);

        context.Dispose();

        var contextEvent = sink.Events
            .SingleOrDefault(model => model.Event.Category == ContextCategory);

        Assert.NotNull(contextEvent);
        Assert.Equal(ContextCategory, contextEvent!.Event.Kind);

        var payload = contextEvent.Event.Payload;
        Assert.True(payload.TryGetValue("name", out var nameObj));
        Assert.Equal("ShrinkToFit", nameObj);

        Assert.True(payload.TryGetValue("values", out var valuesObj));
        var values = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(valuesObj);
        Assert.Equal(140, Assert.IsType<int>(values["availableWidth"]!));
        Assert.Equal(210, Assert.IsType<int>(values["intrinsicWidth"]!));
    }

    private sealed class InMemorySink : IDiagnosticSink
    {
        public string SinkId => "context-sink";

        public List<DiagnosticsModel> Events { get; } = [];

        public void Publish(DiagnosticsModel model)
        {
            Events.Add(model);
        }
    }
}
