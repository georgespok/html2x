namespace Html2x.Abstractions.Diagnostics;

public interface IDiagnosticsHost
{
    void AttachDiagnosticsSession(Func<IDiagnosticSession?> sessionAccessor);
}
