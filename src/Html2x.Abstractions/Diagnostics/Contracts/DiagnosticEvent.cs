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
        StructuredDumpMetadata? dump = null)
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
        Dump = dump;
    }

    public Guid EventId { get; }

    public Guid SessionId { get; }

    public string Category { get; }

    public string Kind { get; }

    public DateTimeOffset Timestamp { get; }

    public IReadOnlyDictionary<string, object?> Payload { get; }

    public StructuredDumpMetadata? Dump { get; }
}

public sealed record StructuredDumpMetadata
{
    public StructuredDumpMetadata(
        string format,
        string summary,
        int nodeCount,
        string body)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Dump format is required.", nameof(format));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Dump summary is required.", nameof(summary));
        }

        if (nodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeCount), "Node count must be non-negative.");
        }

        if (body is null)
        {
            throw new ArgumentNullException(nameof(body));
        }

        Format = format;
        Summary = summary;
        NodeCount = nodeCount;
        Body = body;
    }

    public string Format { get; }

    public string Summary { get; }

    public int NodeCount { get; }

    public string Body { get; }
}

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
