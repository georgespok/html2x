namespace Html2x.Abstractions.Diagnostics;

public sealed class StyleDiagnosticPayload : IDiagnosticsPayload
{
    public string Kind => "style.diagnostic";

    public string PropertyName { get; init; } = string.Empty;

    public string RawValue { get; init; } = string.Empty;

    public string? NormalizedValue { get; init; }

    public string Decision { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public DiagnosticContext? Context { get; init; }
}
