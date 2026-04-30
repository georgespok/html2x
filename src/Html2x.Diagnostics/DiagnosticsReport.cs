using Html2x.Diagnostics.Contracts;

namespace Html2x.Diagnostics;

public sealed class DiagnosticsReport
{
    public DiagnosticsReport(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<DiagnosticRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        StartTime = startTime;
        EndTime = endTime;
        Records = Array.AsReadOnly(records.ToArray());
    }

    public DateTimeOffset StartTime { get; }

    public DateTimeOffset EndTime { get; }

    public IReadOnlyList<DiagnosticRecord> Records { get; }
}
