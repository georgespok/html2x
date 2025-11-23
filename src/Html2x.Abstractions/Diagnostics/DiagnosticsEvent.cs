namespace Html2x.Abstractions.Diagnostics;

public class DiagnosticsEvent
{
    public DiagnosticsEventType Type { get; init; }
    
    public string Name { get; init; } = null!;

    public string? Description { get; init; } = null!;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public IDiagnosticsPayload? Payload { get; init; }
}