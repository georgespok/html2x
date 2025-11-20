namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticEvent
{
    public DiagnosticEvent(
        Guid eventId,
        Guid sessionId,
        string category,
        string kind,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, object?> payload,
        SnapshotMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Event category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            throw new ArgumentException("Event kind is required.", nameof(kind));
        }

        EventId = eventId;
        SessionId = sessionId;
        Category = category;
        Kind = kind;
        Timestamp = timestamp;
        Payload = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        Metadata = metadata;
    }

    public Guid EventId { get; }

    public Guid SessionId { get; }

    public string Category { get; }

    public string Kind { get; }

    public DateTimeOffset Timestamp { get; }

    public IReadOnlyDictionary<string, object?> Payload { get; }

    public SnapshotMetadata? Metadata { get; }
}