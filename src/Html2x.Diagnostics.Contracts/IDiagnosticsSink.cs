namespace Html2x.Diagnostics.Contracts;

public interface IDiagnosticsSink
{
    void Emit(DiagnosticRecord record);
}
