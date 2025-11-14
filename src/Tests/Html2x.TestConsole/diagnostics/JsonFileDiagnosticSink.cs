using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.TestConsole.Diagnostics;

internal sealed class JsonFileDiagnosticSink : IDiagnosticSink
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();

    public JsonFileDiagnosticSink(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string SinkId => "json-file-sink";

    public void Publish(DiagnosticsModel model)
    {
        var entry = new
        {
            session = model.Session,
            @event = model.Event,
            contexts = model.ActiveContexts
        };

        var json = JsonSerializer.Serialize(entry, _options);

        lock (_gate)
        {
            File.AppendAllText(_filePath, json + Environment.NewLine);
        }
    }
}
