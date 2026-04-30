namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticStringValue : DiagnosticValue
{
    public DiagnosticStringValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value;
    }

    public string Value { get; }
}
