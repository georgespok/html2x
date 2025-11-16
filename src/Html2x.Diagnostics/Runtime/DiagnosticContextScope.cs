using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Runtime;

internal sealed class DiagnosticContextScope : IDiagnosticContextScope
{
    private const string ContextCategory = "context/detail";

    private readonly DiagnosticSession _session;
    private readonly Dictionary<string, object?> _values;
    private bool _disposed;

    public DiagnosticContextScope(
        DiagnosticSession session,
        string name,
        IReadOnlyDictionary<string, object?>? seedValues)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Context name is required.", nameof(name));
        }

        _values = seedValues is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(seedValues, StringComparer.Ordinal);

        Snapshot = new DiagnosticContextSnapshot(
            Guid.NewGuid(),
            _session.Descriptor.SessionId,
            name,
            _values,
            DateTimeOffset.UtcNow);
    }

    public DiagnosticContextSnapshot Snapshot { get; private set; }

    public void Set(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        _values[key] = value;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_session.IsEnabled)
        {
            return;
        }

        var closedSnapshot = new DiagnosticContextSnapshot(
            Snapshot.ContextId,
            Snapshot.SessionId,
            Snapshot.Name,
            _values,
            Snapshot.OpenedAt,
            DateTimeOffset.UtcNow);
        Snapshot = closedSnapshot;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["contextId"] = closedSnapshot.ContextId,
            ["name"] = closedSnapshot.Name,
            ["openedAt"] = closedSnapshot.OpenedAt,
            ["values"] = new Dictionary<string, object?>(_values, StringComparer.Ordinal)
        };

        if (closedSnapshot.ClosedAt is DateTimeOffset closedAt)
        {
            payload["closedAt"] = closedAt;
        }

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            closedSnapshot.SessionId,
            ContextCategory,
            ContextCategory,
            DateTimeOffset.UtcNow,
            payload);

        _session.Publish(diagnosticEvent);
    }
}
