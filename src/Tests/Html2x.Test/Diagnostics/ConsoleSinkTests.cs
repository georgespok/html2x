using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Test.Diagnostics;

public sealed class ConsoleSinkTests
{
    private const string ConsoleSinkTypeName = "Html2x.Diagnostics.Sinks.ConsoleDiagnosticSink, Html2x.Diagnostics";
    private const string ConsoleSinkOptionsTypeName = "Html2x.Diagnostics.Sinks.ConsoleDiagnosticSinkOptions, Html2x.Diagnostics";

    [Fact]
    public void ConsoleSink_ShouldEmitStageTimelineMirroringEventOrder()
    {
        using var writer = new StringWriter();
        var sink = CreateConsoleSinkOrFail(writer);

        var session = new DiagnosticSessionDescriptor(
            Guid.NewGuid(),
            "console-contract",
            DateTimeOffset.UtcNow,
            isEnabled: true,
            new DiagnosticSessionConfiguration(),
            new Dictionary<string, object?>(StringComparer.Ordinal));

        var events = new[]
        {
            CreateStageEvent(session.SessionId, "stage/style", "stage/style/start"),
            CreateStageEvent(session.SessionId, "stage/style", "stage/style/stop"),
            CreateStageEvent(session.SessionId, "stage/layout", "stage/layout/start"),
            CreateStageEvent(session.SessionId, "stage/layout", "stage/layout/stop")
        };

        foreach (var diagnosticEvent in events)
        {
            var model = new DiagnosticsModel(session, diagnosticEvent, []);
            sink.Publish(model);
        }

        var lines = writer
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        var expected = new[]
        {
            "[Diagnostics] stage/style stage/style/start",
            "[Diagnostics] stage/style stage/style/stop",
            "[Diagnostics] stage/layout stage/layout/start",
            "[Diagnostics] stage/layout stage/layout/stop"
        };

        Assert.True(lines.Length >= expected.Length, "Console sink did not emit enough lines.");

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.StartsWith(expected[i], lines[i], StringComparison.Ordinal);
        }
    }

    private static DiagnosticEvent CreateStageEvent(Guid sessionId, string category, string kind)
    {
        return new DiagnosticEvent(
            Guid.NewGuid(),
            sessionId,
            category,
            kind,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(StringComparer.Ordinal));
    }

    private static IDiagnosticSink CreateConsoleSinkOrFail(TextWriter writer)
    {
        var sinkType = Type.GetType(ConsoleSinkTypeName);
        Assert.NotNull(sinkType);

        var optionsType = Type.GetType(ConsoleSinkOptionsTypeName);
        Assert.NotNull(optionsType);

        object? optionsInstance;
        try
        {
            optionsInstance = Activator.CreateInstance(optionsType!, writer);
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("ConsoleDiagnosticSinkOptions must provide a constructor accepting a TextWriter.");
        }

        Assert.NotNull(optionsInstance);

        object? sinkInstance;
        try
        {
            sinkInstance = Activator.CreateInstance(sinkType!, optionsInstance);
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("ConsoleDiagnosticSink must provide a constructor accepting ConsoleDiagnosticSinkOptions.");
        }

        return Assert.IsAssignableFrom<IDiagnosticSink>(sinkInstance);
    }
}
