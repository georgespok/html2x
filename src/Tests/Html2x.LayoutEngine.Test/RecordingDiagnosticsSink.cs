using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Test;

internal sealed class RecordingDiagnosticsSink : IDiagnosticsSink
{
    private readonly List<DiagnosticRecord> _records = [];

    public IReadOnlyList<DiagnosticRecord> Records => _records;

    public void Emit(DiagnosticRecord record)
    {
        _records.Add(record);
    }
}