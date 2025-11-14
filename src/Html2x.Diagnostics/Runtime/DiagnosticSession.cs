using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Pipeline;

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
        return new DiagnosticContextScope(name, Descriptor.SessionId, seedValues);
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

        var model = new DiagnosticsModel(
            Descriptor,
            diagnosticEvent,
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
            Array.Empty<DiagnosticContextSnapshot>());

        _dispatcher.Dispatch(model);
    }
}
