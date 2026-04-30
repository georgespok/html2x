namespace Html2x.Diagnostics.Contracts;

public abstract record DiagnosticValue
{
    public static DiagnosticValue From(string value) => new DiagnosticStringValue(value);

    public static DiagnosticValue From(int value) => new DiagnosticNumberValue(value);

    public static DiagnosticValue From(long value) => new DiagnosticNumberValue(value);

    public static DiagnosticValue From(float value) => new DiagnosticNumberValue(value);

    public static DiagnosticValue From(double value) => new DiagnosticNumberValue(value);

    public static DiagnosticValue From(decimal value) => new DiagnosticNumberValue(decimal.ToDouble(value));

    public static DiagnosticValue From(bool value) => new DiagnosticBooleanValue(value);

    public static DiagnosticValue FromEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        new DiagnosticStringValue(value.ToString());

    public static implicit operator DiagnosticValue(string value) => From(value);

    public static implicit operator DiagnosticValue(int value) => From(value);

    public static implicit operator DiagnosticValue(long value) => From(value);

    public static implicit operator DiagnosticValue(float value) => From(value);

    public static implicit operator DiagnosticValue(double value) => From(value);

    public static implicit operator DiagnosticValue(decimal value) => From(value);

    public static implicit operator DiagnosticValue(bool value) => From(value);
}
