namespace Html2x.Diagnostics.Contracts;

public sealed class NullDiagnosticsSink : IDiagnosticsSink
{
    private NullDiagnosticsSink()
    {
    }

    public static NullDiagnosticsSink Instance { get; } = new();

    public void Emit(DiagnosticRecord record)
    {
    }
}