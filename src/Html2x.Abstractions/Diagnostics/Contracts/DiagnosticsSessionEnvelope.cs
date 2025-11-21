namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticsSessionEnvelope
{
    public DiagnosticsSessionEnvelope(
        Guid sessionId,
        string correlationId,
        string pipelineName,
        IReadOnlyDictionary<string, string?>? environmentMarkers,
        DateTimeOffset startTimestamp,
        DateTimeOffset? endTimestamp,
        DiagnosticsSessionStatus status,
        IReadOnlyList<DiagnosticEvent>? events)
    {
        if (string.IsNullOrWhiteSpace(pipelineName))
        {
            throw new ArgumentException("Pipeline name is required.", nameof(pipelineName));
        }

        SessionId = sessionId;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId;
        PipelineName = pipelineName;
        EnvironmentMarkers = environmentMarkers ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
        Status = status;
        Events = events ?? [];
    }

    public Guid SessionId { get; }

    public string CorrelationId { get; }

    public string PipelineName { get; }

    public IReadOnlyDictionary<string, string?> EnvironmentMarkers { get; }

    public DateTimeOffset StartTimestamp { get; }

    public DateTimeOffset? EndTimestamp { get; }

    public DiagnosticsSessionStatus Status { get; }

    public IReadOnlyList<DiagnosticEvent> Events { get; }
}
