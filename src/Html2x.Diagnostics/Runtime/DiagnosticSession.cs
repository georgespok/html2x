using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Pipeline;
using Html2x.Diagnostics.Snapshot;

namespace Html2x.Diagnostics.Runtime;

internal sealed class DiagnosticSession : IDiagnosticSession
{
    private readonly DiagnosticsDispatcher _dispatcher;
    private readonly Action<IDiagnosticSession> _onDispose;
    private bool _disposed;

    public DiagnosticSession(
        DiagnosticSessionDescriptor descriptor,
        DiagnosticsDispatcher dispatcher,
        Action<IDiagnosticSession> onDispose)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

        PublishLifecycleEvent("start");
    }

    public DiagnosticSessionDescriptor Descriptor { get; }

    public bool IsEnabled => _dispatcher.HasSinks;

    public IDiagnosticContextScope Context(
        string name,
        IReadOnlyDictionary<string, object?>? seedValues = null)
    {
        return new DiagnosticContextScope(this, name, seedValues);
    }

    public void Publish(DiagnosticEvent diagnosticEvent)
    {
        if (diagnosticEvent is null)
        {
            throw new ArgumentNullException(nameof(diagnosticEvent));
        }

        if (_disposed || !IsEnabled)
        {
            return;
        }

        var enrichedEvent = AttachDiagnosticsSnapshot(diagnosticEvent);

        var model = new DiagnosticsModel(
            Descriptor,
            enrichedEvent,
            []);

        _dispatcher.Dispatch(model);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        PublishLifecycleEvent("stop");
        _onDispose(this);
    }

    private void PublishLifecycleEvent(string kind)
    {
        if (!IsEnabled)
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["name"] = Descriptor.Name,
            ["kind"] = kind
        };

        if (Descriptor.Metadata.Count > 0)
        {
            payload["metadata"] = Descriptor.Metadata;
        }

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            Descriptor.SessionId,
            "session",
            $"session/{kind}",
            DateTimeOffset.UtcNow,
            payload);

        var model = new DiagnosticsModel(
            Descriptor,
            diagnosticEvent,
            []);

        _dispatcher.Dispatch(model);
    }

    private static DiagnosticEvent AttachDiagnosticsSnapshot(DiagnosticEvent diagnosticEvent)
    {
        SnapshotDocument? snapshotDocument = null;

        foreach (var entry in diagnosticEvent.Payload)
        {
            if (entry.Value is SnapshotDocument document)
            {
                snapshotDocument ??= document;
                break;
            }
        }

        if (snapshotDocument is null)
        {
            return diagnosticEvent;
        }

        var payload = new Dictionary<string, object?>(diagnosticEvent.Payload.Count, StringComparer.Ordinal);

        foreach (var entry in diagnosticEvent.Payload)
        {
            if (entry.Value is SnapshotDocument)
            {
                continue;
            }

            payload[entry.Key] = entry.Value;
        }
        var metadata = SnapshotSerializer.Serialize(snapshotDocument);

        return new DiagnosticEvent(
            diagnosticEvent.EventId,
            diagnosticEvent.SessionId,
            diagnosticEvent.Category,
            diagnosticEvent.Kind,
            diagnosticEvent.Timestamp,
            payload,
            metadata);
    }
}
