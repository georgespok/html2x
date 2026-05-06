namespace Html2x.Diagnostics.Contracts;

public sealed record DiagnosticNumberValue : DiagnosticValue
{
    public DiagnosticNumberValue(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Diagnostic numbers must be finite.");
        }

        Value = value;
    }

    public double Value { get; }
}