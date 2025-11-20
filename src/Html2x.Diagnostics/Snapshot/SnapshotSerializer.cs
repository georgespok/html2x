using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Snapshot;

internal static class SnapshotSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static SnapshotMetadata Serialize(SnapshotDocument document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var payload = new
        {
            category = document.Category,
            summary = document.Summary,
            nodeCount = document.NodeCount,
            nodes = document.Nodes
        };

        var body = JsonSerializer.Serialize(payload, SerializerOptions);

        return new SnapshotMetadata(
            "json",
            document.Summary,
            document.NodeCount,
            body);
    }
}
