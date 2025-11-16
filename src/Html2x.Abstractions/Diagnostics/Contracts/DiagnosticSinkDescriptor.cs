namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticSinkDescriptor
{
    public DiagnosticSinkDescriptor(
        string sinkId,
        string type,
        DiagnosticSinkStatus status,
        IReadOnlyDictionary<string, object?> configuration)
    {
        if (string.IsNullOrWhiteSpace(sinkId))
        {
            throw new ArgumentException("Sink identifier is required.", nameof(sinkId));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Sink type is required.", nameof(type));
        }

        SinkId = sinkId;
        Type = type;
        Status = status;
        Configuration = configuration ?? new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    public string SinkId { get; }

    public string Type { get; }

    public DiagnosticSinkStatus Status { get; }

    public IReadOnlyDictionary<string, object?> Configuration { get; }
}

public enum DiagnosticSinkStatus
{
    Active = 0,
    Disabled = 1,
    CircuitBroken = 2
}
