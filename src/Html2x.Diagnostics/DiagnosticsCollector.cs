using Html2x.Diagnostics.Contracts;

namespace Html2x.Diagnostics;

public sealed class DiagnosticsCollector(DateTimeOffset startTime) : IDiagnosticsSink
{
    private readonly object _gate = new();
    private readonly List<DiagnosticRecord> _records = [];

    public DiagnosticsCollector()
        : this(DateTimeOffset.UtcNow)
    {
    }

    public void Emit(DiagnosticRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_gate)
        {
            _records.Add(record);
        }
    }

    public DiagnosticsReport ToReport() => ToReport(DateTimeOffset.UtcNow);

    public DiagnosticsReport ToReport(DateTimeOffset endTime)
    {
        DiagnosticRecord[] records;
        lock (_gate)
        {
            records = _records.ToArray();
        }

        return new(startTime, endTime, records);
    }
}