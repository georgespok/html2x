namespace Html2x.Abstractions.Diagnostics;

public static class DiagnosticsEventFactory
{
    public static DiagnosticsEvent StageStarted(string stageName, IDiagnosticsPayload? payload = null)
    {
        return CreateStageEvent(
            DiagnosticsEventType.StartStage,
            stageName,
            DiagnosticStageState.Started,
            DiagnosticSeverity.Info,
            description: null,
            payload);
    }

    public static DiagnosticsEvent StageSucceeded(string stageName, IDiagnosticsPayload? payload = null)
    {
        return CreateStageEvent(
            DiagnosticsEventType.EndStage,
            stageName,
            DiagnosticStageState.Succeeded,
            DiagnosticSeverity.Info,
            description: null,
            payload);
    }

    public static DiagnosticsEvent StageFailed(
        string stageName,
        string? failureReason,
        IDiagnosticsPayload? payload = null)
    {
        return CreateStageEvent(
            DiagnosticsEventType.Error,
            stageName,
            DiagnosticStageState.Failed,
            DiagnosticSeverity.Error,
            failureReason,
            payload);
    }

    public static DiagnosticsEvent StageSkipped(
        string stageName,
        string? reason,
        IDiagnosticsPayload? payload = null)
    {
        return CreateStageEvent(
            DiagnosticsEventType.EndStage,
            stageName,
            DiagnosticStageState.Skipped,
            DiagnosticSeverity.Info,
            reason,
            payload);
    }

    private static DiagnosticsEvent CreateStageEvent(
        DiagnosticsEventType type,
        string stageName,
        DiagnosticStageState state,
        DiagnosticSeverity severity,
        string? description,
        IDiagnosticsPayload? payload)
    {
        return new DiagnosticsEvent
        {
            Type = type,
            Name = stageName,
            StageState = state,
            Severity = severity,
            Description = description,
            Payload = payload
        };
    }
}
