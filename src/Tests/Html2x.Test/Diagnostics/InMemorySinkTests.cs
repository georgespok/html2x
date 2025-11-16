using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Test.Diagnostics;

public sealed class InMemorySinkTests
{
    private const string SinkTypeName = "Html2x.Diagnostics.Sinks.InMemoryDiagnosticSink, Html2x.Diagnostics";

    [Fact]
    public void InMemorySink_ShouldCapturePublishedModelsForDeterministicAssertions()
    {
        var sink = CreateSinkOrFail();

        var session = new DiagnosticSessionDescriptor(
            Guid.NewGuid(),
            "in-memory-contract",
            DateTimeOffset.UtcNow,
            isEnabled: true,
            new DiagnosticSessionConfiguration(),
            new Dictionary<string, object?>(StringComparer.Ordinal));

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            session.SessionId,
            "stage/style",
            "stage/style/start",
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["note"] = "determinism"
            });

        var model = new DiagnosticsModel(session, diagnosticEvent, Array.Empty<DiagnosticContextSnapshot>());

        sink.Publish(model);

        var entries = GetEntries(sink);
        Assert.Single(entries);
        Assert.Equal(diagnosticEvent.EventId, entries[0].Event.EventId);
        Assert.Equal("determinism", entries[0].Event.Payload["note"]);
    }

    private static IDiagnosticSink CreateSinkOrFail()
    {
        var sinkType = Type.GetType(SinkTypeName);
        Assert.NotNull(sinkType);

        object? instance;
        try
        {
            instance = Activator.CreateInstance(sinkType!);
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("InMemoryDiagnosticSink must expose a public parameterless constructor.");
        }

        return Assert.IsAssignableFrom<IDiagnosticSink>(instance);
    }

    private static IReadOnlyList<DiagnosticsModel> GetEntries(IDiagnosticSink sink)
    {
        var property = sink.GetType().GetProperty("Entries");
        Assert.NotNull(property);

        var value = property.GetValue(sink);
        Assert.NotNull(value);

        return Assert.IsAssignableFrom<IReadOnlyList<DiagnosticsModel>>(value);
    }
}
