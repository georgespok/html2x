namespace Html2x.Diagnostics.Contracts;

public static class DiagnosticStageEmitter
{
    private const string StartedEvent = "stage/started";
    private const string SucceededEvent = "stage/succeeded";
    private const string FailedEvent = "stage/failed";
    private const string SkippedEvent = "stage/skipped";
    private const string CancelledEvent = "stage/cancelled";

    public static void Started(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        Emit(diagnosticsSink, stage, StartedEvent, DiagnosticSeverity.Info, null, fields);

    public static void Succeeded(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        Emit(diagnosticsSink, stage, SucceededEvent, DiagnosticSeverity.Info, null, fields);

    public static void Failed(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, FailedEvent, DiagnosticSeverity.Error, message);

    public static void Skipped(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, SkippedEvent, DiagnosticSeverity.Info, message);

    public static void Cancelled(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, CancelledEvent, DiagnosticSeverity.Info, message);

    public static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        string name,
        DiagnosticSeverity severity,
        string? message,
        DiagnosticFields? fields = null)
    {
        diagnosticsSink?.Emit(new(
            stage,
            name,
            severity,
            message,
            null,
            fields ?? DiagnosticFields.Empty,
            DateTimeOffset.UtcNow));
    }
}