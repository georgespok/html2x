using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Sinks;

public sealed class InMemoryDiagnosticSink : IDiagnosticSink
{
    private readonly object _gate = new();
    private readonly Queue<DiagnosticsModel> _buffer;

    public InMemoryDiagnosticSink()
        : this("in-memory-sink")
    {
    }

    public InMemoryDiagnosticSink(string sinkId, int capacity = 1024)
    {
        if (string.IsNullOrWhiteSpace(sinkId))
        {
            throw new ArgumentException("Sink identifier is required.", nameof(sinkId));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        SinkId = sinkId;
        Capacity = capacity;
        _buffer = new Queue<DiagnosticsModel>(capacity);
    }

    public string SinkId { get; }

    public int Capacity { get; }

    public IReadOnlyList<DiagnosticsModel> Entries
    {
        get
        {
            lock (_gate)
            {
                return _buffer.ToArray();
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _buffer.Clear();
        }
    }

    public void Publish(DiagnosticsModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        lock (_gate)
        {
            if (_buffer.Count == Capacity)
            {
                _buffer.Dequeue();
            }

            _buffer.Enqueue(model);
        }
    }
}
