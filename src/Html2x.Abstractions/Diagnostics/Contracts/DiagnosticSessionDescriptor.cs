namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticSessionDescriptor
{
    public DiagnosticSessionDescriptor(
        Guid sessionId,
        string name,
        DateTimeOffset startedAt,
        bool isEnabled,
        DiagnosticSessionConfiguration configuration,
        IReadOnlyDictionary<string, object?> metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Session name is required.", nameof(name));
        }

        SessionId = sessionId;
        Name = name;
        StartedAt = startedAt;
        IsEnabled = isEnabled;
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Metadata = metadata ?? new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    public Guid SessionId { get; }

    public string Name { get; }

    public DateTimeOffset StartedAt { get; }

    public bool IsEnabled { get; }

    public DiagnosticSessionConfiguration Configuration { get; }

    public IReadOnlyDictionary<string, object?> Metadata { get; }
}

public sealed record DiagnosticSessionConfiguration
{
    private static readonly IReadOnlyList<DiagnosticSinkDescriptor> EmptySinks =
        [];

    public DiagnosticSessionConfiguration(
        IReadOnlyList<DiagnosticSinkDescriptor>? sinks = null,
        bool propagateSinkExceptions = true)
    {
        Sinks = sinks ?? EmptySinks;
        PropagateSinkExceptions = propagateSinkExceptions;
    }

    public IReadOnlyList<DiagnosticSinkDescriptor> Sinks { get; }

    public bool PropagateSinkExceptions { get; }
}
