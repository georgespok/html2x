namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticRecord(
    string Stage,
    string Name,
    DiagnosticSeverity Severity,
    string? Message,
    DiagnosticContext? Context,
    DiagnosticFields Fields,
    DateTimeOffset Timestamp);