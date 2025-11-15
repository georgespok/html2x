namespace Html2x.Diagnostics.Sinks;

public sealed class ConsoleDiagnosticSinkOptions
{
    public ConsoleDiagnosticSinkOptions(TextWriter writer)
        : this("console-sink", writer)
    {
    }

    public ConsoleDiagnosticSinkOptions(string sinkId, TextWriter writer)
    {
        if (string.IsNullOrWhiteSpace(sinkId))
        {
            throw new ArgumentException("Sink identifier is required.", nameof(sinkId));
        }

        Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        SinkId = sinkId;
    }

    public string SinkId { get; }

    public TextWriter Writer { get; }

    public bool IncludeTimestamp { get; set; } = true;

    public bool IncludeSessionName { get; set; } = false;

    internal ConsoleDiagnosticSinkOptions Clone()
    {
        return new ConsoleDiagnosticSinkOptions(SinkId, Writer)
        {
            IncludeTimestamp = IncludeTimestamp,
            IncludeSessionName = IncludeSessionName
        };
    }
}
