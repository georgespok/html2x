using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Sinks;

public sealed class JsonDiagnosticSink : IDiagnosticSink
{
    private readonly JsonDiagnosticSinkOptions _options;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly object _gate = new();
    private bool _hasEntries;

    public JsonDiagnosticSink(JsonDiagnosticSinkOptions options)
    {
        _options = options?.Clone() ?? throw new ArgumentNullException(nameof(options));

        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = _options.WriteIndented
        };

        var directory = Path.GetDirectoryName(_options.OutputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!_options.Append && File.Exists(_options.OutputPath))
        {
            File.Delete(_options.OutputPath);
        }
        else if (_options.Append && File.Exists(_options.OutputPath))
        {
            _hasEntries = DetectExistingEntries(_options.OutputPath);
        }
    }

    public string SinkId => _options.SinkId;

    public void Publish(DiagnosticsModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var entry = CreateEntry(model);
        var json = JsonSerializer.Serialize(entry, _serializerOptions);

        lock (_gate)
        {
            if (!_hasEntries && !_options.Append)
            {
                WriteNewFile(json);
                _hasEntries = true;
                return;
            }

            if (!_hasEntries && File.Exists(_options.OutputPath))
            {
                // File exists but detected as empty; rewrite fresh.
                WriteNewFile(json);
                _hasEntries = true;
                return;
            }

            if (!File.Exists(_options.OutputPath))
            {
                WriteNewFile(json);
                _hasEntries = true;
                return;
            }

            AppendEntry(json);
            _hasEntries = true;
        }
    }

    private object CreateEntry(DiagnosticsModel model)
    {
        var eventPayload = model.Event;

        if (eventPayload.Dump is not null)
        {
            using var document = JsonDocument.Parse(eventPayload.Dump.Body);
            var dumpBody = document.RootElement.Clone();

            return new
            {
                session = model.Session,
                @event = new
                {
                    eventPayload.EventId,
                    eventPayload.SessionId,
                    eventPayload.Category,
                    eventPayload.Kind,
                    eventPayload.Timestamp,
                    eventPayload.Payload,
                    dump = new
                    {
                        eventPayload.Dump.Format,
                        eventPayload.Dump.Summary,
                        eventPayload.Dump.NodeCount,
                        body = dumpBody
                    }
                },
                contexts = _options.IncludeContexts ? model.ActiveContexts : []
            };
        }

        return new
        {
            session = model.Session,
            @event = eventPayload,
            contexts = _options.IncludeContexts ? model.ActiveContexts : []
        };
    }

    private void WriteNewFile(string json)
    {
        var payload = $"[{Environment.NewLine}{json}{Environment.NewLine}]";
        File.WriteAllText(_options.OutputPath, payload, Encoding.UTF8);
    }

    private void AppendEntry(string json)
    {
        using var stream = new FileStream(
            _options.OutputPath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.Read);

        var insertionPosition = FindClosingBracketPosition(stream);

        stream.SetLength(insertionPosition);
        stream.Seek(insertionPosition, SeekOrigin.Begin);

        var buffer = Encoding.UTF8.GetBytes($",{Environment.NewLine}{json}{Environment.NewLine}]");
        stream.Write(buffer, 0, buffer.Length);
    }

    private static long FindClosingBracketPosition(FileStream stream)
    {
        var buffer = new byte[1];
        for (var offset = 1; offset <= stream.Length; offset++)
        {
            stream.Seek(-offset, SeekOrigin.End);
            var read = stream.Read(buffer, 0, 1);
            if (read == 0)
            {
                break;
            }

            var ch = (char)buffer[0];
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (ch != ']')
            {
                throw new InvalidDataException("JsonDiagnosticSink output is corrupted (missing array terminator).");
            }

            return stream.Position - 1;
        }

        throw new InvalidDataException("JsonDiagnosticSink output is corrupted (no closing bracket found).");
    }

    private static bool DetectExistingEntries(string path)
    {
        try
        {
            var content = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Array
                   && document.RootElement.GetArrayLength() > 0;
        }
        catch
        {
            return false;
        }
    }
}
