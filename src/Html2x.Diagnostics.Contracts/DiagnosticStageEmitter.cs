namespace Html2x.Diagnostics.Contracts;

public static class DiagnosticStageEmitter
{
    public static void Started(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        Emit(diagnosticsSink, stage, "stage/started", DiagnosticSeverity.Info, null, fields);

    public static void Succeeded(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        DiagnosticFields? fields = null) =>
        Emit(diagnosticsSink, stage, "stage/succeeded", DiagnosticSeverity.Info, null, fields);

    public static void Failed(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, "stage/failed", DiagnosticSeverity.Error, message);

    public static void Skipped(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, "stage/skipped", DiagnosticSeverity.Info, message);

    public static void Cancelled(IDiagnosticsSink? diagnosticsSink, string stage, string message) =>
        Emit(diagnosticsSink, stage, "stage/cancelled", DiagnosticSeverity.Info, message);

    public static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        string stage,
        string name,
        DiagnosticSeverity severity,
        string? message,
        DiagnosticFields? fields = null)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: stage,
            Name: name,
            Severity: severity,
            Message: message,
            Context: null,
            Fields: fields ?? DiagnosticFields.Empty,
            Timestamp: DateTimeOffset.UtcNow));
    }
}
