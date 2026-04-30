namespace Html2x.Diagnostics.Contracts;

public sealed class NullDiagnosticsSink : IDiagnosticsSink
{
    public static NullDiagnosticsSink Instance { get; } = new();

    private NullDiagnosticsSink()
    {
    }

    public void Emit(DiagnosticRecord record)
    {
    }
}
