namespace Html2x.Abstractions.Diagnostics;

public class DiagnosticsEvent
{
    public DiagnosticsEventType Type { get; init; }
    
    public string Name { get; init; } = null!;

    public string? Description { get; init; } = null!;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public DiagnosticSeverity? Severity { get; init; }

    public DiagnosticStageState? StageState { get; init; }

    public DiagnosticContext? Context { get; init; }

    public string? RawUserInput { get; init; }

    public IDiagnosticsPayload? Payload { get; init; }
}
