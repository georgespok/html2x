using Html2x.Diagnostics.Contracts;

namespace Html2x.Diagnostics;

public sealed class DiagnosticsCollector : IDiagnosticsSink
{
    private readonly object _gate = new();
    private readonly List<DiagnosticRecord> _records = [];
    private readonly DateTimeOffset _startTime;

    public DiagnosticsCollector()
        : this(DateTimeOffset.UtcNow)
    {
    }

    public DiagnosticsCollector(DateTimeOffset startTime)
    {
        _startTime = startTime;
    }

    public void Emit(DiagnosticRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_gate)
        {
            _records.Add(record);
        }
    }

    public DiagnosticsReport ToReport()
    {
        return ToReport(DateTimeOffset.UtcNow);
    }

    public DiagnosticsReport ToReport(DateTimeOffset endTime)
    {
        DiagnosticRecord[] records;
        lock (_gate)
        {
            records = _records.ToArray();
        }

        return new DiagnosticsReport(_startTime, endTime, records);
    }
}
