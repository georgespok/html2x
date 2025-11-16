namespace Html2x.Diagnostics.Sinks;

public sealed class JsonDiagnosticSinkOptions
{
    public JsonDiagnosticSinkOptions(string outputPath)
        : this("json-file", outputPath)
    {
    }

    public JsonDiagnosticSinkOptions(string sinkId, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(sinkId))
        {
            throw new ArgumentException("Sink identifier is required.", nameof(sinkId));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        SinkId = sinkId;
        OutputPath = outputPath;
    }

    public string SinkId { get; }

    public string OutputPath { get; }

    public bool Append { get; set; }

    public bool WriteIndented { get; set; }

    public bool IncludeContexts { get; set; } = true;

    internal JsonDiagnosticSinkOptions Clone()
    {
        return new JsonDiagnosticSinkOptions(SinkId, OutputPath)
        {
            Append = Append,
            WriteIndented = WriteIndented,
            IncludeContexts = IncludeContexts
        };
    }
}
