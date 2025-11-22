using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics;

public static class DiagnosticsSessionSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(DiagnosticsSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var envelope = new DiagnosticsSessionEnvelope
        {
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Options = session.Options,
            Events = session.Events.Select(MapEvent).ToArray()
        };

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    private static DiagnosticsEventEnvelope MapEvent(DiagnosticsEvent @event)
    {
        return new DiagnosticsEventEnvelope
        {
            Type = @event.Type,
            Name = @event.Name,
            Timestamp = @event.Timestamp,
            Payload = MapPayload(@event.Payload)
        };
    }

    private static object? MapPayload(IDiagnosticsPayload? payload)
    {
        return payload switch
        {
            HtmlPayload html => new
            {
                html.Kind,
                html.Html
            },
            LayoutSnapshotPayload layout => new
            {
                layout.Kind,
                layout.Snapshot
            },
            RenderSummaryPayload summary => new
            {
                summary.Kind,
                summary.PageCount,
                summary.PdfSize
            },
            null => null,
            _ => new { payload.Kind }
        };
    }

    private sealed class DiagnosticsSessionEnvelope
    {
        public DateTimeOffset StartTime { get; init; }

        public DateTimeOffset EndTime { get; init; }

        public object Options { get; init; } = null!;

        public IReadOnlyList<DiagnosticsEventEnvelope> Events { get; init; } = Array.Empty<DiagnosticsEventEnvelope>();
    }

    private sealed class DiagnosticsEventEnvelope
    {
        public DiagnosticsEventType Type { get; init; }

        public string Name { get; init; } = string.Empty;

        public DateTimeOffset Timestamp { get; init; }

        public object? Payload { get; init; }
    }
}
