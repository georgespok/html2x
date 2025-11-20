using System.Text.Json;
using System.Text.Json.Serialization;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Serialization;

internal static class DiagnosticsSessionEnvelopeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        SkipValidation = false
    };

    public static byte[] Serialize(DiagnosticsSessionEnvelope envelope)
    {
        using var stream = new MemoryStream();
        Serialize(envelope, stream);
        return stream.ToArray();
    }

    public static void Serialize(DiagnosticsSessionEnvelope envelope, Stream destination)
    {
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        var normalized = NormalizeEnvelope(envelope);

        using var writer = new Utf8JsonWriter(destination, WriterOptions);
        JsonSerializer.Serialize(writer, normalized, SerializerOptions);
    }

    private static DiagnosticsSessionEnvelope NormalizeEnvelope(DiagnosticsSessionEnvelope envelope)
    {
        var environmentMarkers = envelope.EnvironmentMarkers.Count == 0
            ? envelope.EnvironmentMarkers
            : new SortedDictionary<string, string?>(
                envelope.EnvironmentMarkers.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var events = envelope.Events.Count == 0
            ? envelope.Events
            : envelope.Events.Select(NormalizeEvent).ToList();

        return new DiagnosticsSessionEnvelope(
            envelope.SessionId,
            envelope.CorrelationId,
            envelope.PipelineName,
            environmentMarkers,
            envelope.StartTimestamp,
            envelope.EndTimestamp,
            envelope.Status,
            events);
    }

    private static DiagnosticEvent NormalizeEvent(DiagnosticEvent diagnosticEvent)
    {
        var payload = diagnosticEvent.Payload.Count == 0
            ? diagnosticEvent.Payload
            : new SortedDictionary<string, object?>(
                diagnosticEvent.Payload.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        return new DiagnosticEvent(
            diagnosticEvent.EventId,
            diagnosticEvent.SessionId,
            diagnosticEvent.Category,
            diagnosticEvent.Kind,
            diagnosticEvent.Timestamp,
            payload,
            diagnosticEvent.Metadata);
    }
}
