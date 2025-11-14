using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Abstractions.Diagnostics;

public readonly struct DiagnosticsStageScope : IDisposable
{
    private readonly IDiagnosticSession? _session;
    private readonly string _stage;

    private DiagnosticsStageScope(IDiagnosticSession? session, string stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            throw new ArgumentException("Stage name is required.", nameof(stage));
        }

        if (session is not { IsEnabled: true })
        {
            _session = null;
            _stage = stage;
            return;
        }

        _session = session;
        _stage = stage;
        Publish("start");
    }

    public static DiagnosticsStageScope Begin(IDiagnosticSession? session, string stage)
        => new(session, stage);

    public void Dispose()
    {
        if (_session is null)
        {
            return;
        }

        Publish("stop");
    }

    private void Publish(string kind)
    {
        if (_session is null)
        {
            return;
        }

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            _session.Descriptor.SessionId,
            _stage,
            $"{_stage}/{kind}",
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(StringComparer.Ordinal));

        _session.Publish(diagnosticEvent);
    }
}
