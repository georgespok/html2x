using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Diagnostics.Contracts;

namespace Html2x.Diagnostics;

public static class DiagnosticsReportSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(DiagnosticsReport report)
    {
        return JsonSerializer.Serialize(ToSerializableObject(report), JsonOptions);
    }

    public static object ToSerializableObject(DiagnosticsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new DiagnosticsReportEnvelope
        {
            StartTime = report.StartTime,
            EndTime = report.EndTime,
            Records = report.Records.Select(MapRecord).ToArray()
        };
    }

    private static DiagnosticsRecordEnvelope MapRecord(DiagnosticRecord record)
    {
        return new DiagnosticsRecordEnvelope
        {
            Stage = record.Stage,
            Name = record.Name,
            Severity = record.Severity.ToString(),
            Message = record.Message,
            Context = MapContext(record.Context),
            Fields = MapFields(record.Fields),
            Timestamp = record.Timestamp
        };
    }

    private static DiagnosticsContextEnvelope? MapContext(DiagnosticContext? context)
    {
        return context is null
            ? null
            : new DiagnosticsContextEnvelope
            {
                Selector = context.Selector,
                ElementIdentity = context.ElementIdentity,
                StyleDeclaration = context.StyleDeclaration,
                StructuralPath = context.StructuralPath,
                RawUserInput = context.RawUserInput
            };
    }

    private static IReadOnlyDictionary<string, object?> MapFields(DiagnosticFields fields)
    {
        return fields.ToDictionary(
            static field => field.Key,
            static field => MapValue(field.Value),
            StringComparer.Ordinal);
    }

    private static object? MapValue(DiagnosticValue? value)
    {
        return value switch
        {
            null => null,
            DiagnosticStringValue stringValue => stringValue.Value,
            DiagnosticNumberValue numberValue => numberValue.Value,
            DiagnosticBooleanValue booleanValue => booleanValue.Value,
            DiagnosticArray arrayValue => arrayValue.Select(MapValue).ToArray(),
            DiagnosticObject objectValue => objectValue.ToDictionary(
                static field => field.Key,
                static field => MapValue(field.Value),
                StringComparer.Ordinal),
            _ => throw new NotSupportedException($"Unsupported diagnostic value type '{value.GetType().FullName}'.")
        };
    }

    private sealed class DiagnosticsReportEnvelope
    {
        public DateTimeOffset StartTime { get; init; }

        public DateTimeOffset EndTime { get; init; }

        public IReadOnlyList<DiagnosticsRecordEnvelope> Records { get; init; } = Array.Empty<DiagnosticsRecordEnvelope>();
    }

    private sealed class DiagnosticsRecordEnvelope
    {
        public string Stage { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Severity { get; init; } = string.Empty;

        public string? Message { get; init; }

        public DiagnosticsContextEnvelope? Context { get; init; }

        public IReadOnlyDictionary<string, object?> Fields { get; init; } = new Dictionary<string, object?>();

        public DateTimeOffset Timestamp { get; init; }
    }

    private sealed class DiagnosticsContextEnvelope
    {
        public string? Selector { get; init; }

        public string? ElementIdentity { get; init; }

        public string? StyleDeclaration { get; init; }

        public string? StructuralPath { get; init; }

        public string? RawUserInput { get; init; }
    }
}
