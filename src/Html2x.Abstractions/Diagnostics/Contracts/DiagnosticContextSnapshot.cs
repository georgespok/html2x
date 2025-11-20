namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticContextSnapshot
{
    public DiagnosticContextSnapshot(
        Guid contextId,
        Guid sessionId,
        string name,
        IReadOnlyDictionary<string, object?> values,
        DateTimeOffset openedAt,
        DateTimeOffset? closedAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Context name is required.", nameof(name));
        }

        ContextId = contextId;
        SessionId = sessionId;
        Name = name;
        Values = values ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        OpenedAt = openedAt;
        ClosedAt = closedAt;
    }

    public Guid ContextId { get; }

    public Guid SessionId { get; }

    public string Name { get; }

    public IReadOnlyDictionary<string, object?> Values { get; }

    public DateTimeOffset OpenedAt { get; }

    public DateTimeOffset? ClosedAt { get; }
}