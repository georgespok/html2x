using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Runtime;

internal sealed class DiagnosticContextScope : IDiagnosticContextScope
{
    private readonly Dictionary<string, object?> _values;

    public DiagnosticContextScope(
        string name,
        Guid sessionId,
        IReadOnlyDictionary<string, object?>? seedValues)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Context name is required.", nameof(name));
        }

        _values = seedValues is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(seedValues, StringComparer.Ordinal);

        Snapshot = new DiagnosticContextSnapshot(
            Guid.NewGuid(),
            sessionId,
            name,
            _values,
            DateTimeOffset.UtcNow);
    }

    public DiagnosticContextSnapshot Snapshot { get; }

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
        // Context completion events will be implemented in later tasks.
    }
}
