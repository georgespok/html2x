using System.Text.Json;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Test.Diagnostics;

public sealed class SinkContractTests
{
    private const string JsonSinkType = "Html2x.Diagnostics.Sinks.JsonDiagnosticSink, Html2x.Diagnostics";
    private const string JsonSinkOptionsType = "Html2x.Diagnostics.Sinks.JsonDiagnosticSinkOptions, Html2x.Diagnostics";

    [Fact]
    public void JsonSink_ShouldPersistSessionsEventsDumpsAndContexts()
    {
        using var temp = new TempDirectory();
        var outputPath = Path.Combine(temp.Path, "session.json");

        var sink = CreateJsonSinkOrFail(outputPath);

        var sessionDescriptor = new DiagnosticSessionDescriptor(
            Guid.NewGuid(),
            "json-contract",
            DateTimeOffset.UtcNow,
            isEnabled: true,
            new DiagnosticSessionConfiguration(),
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["tenant"] = "alpha"
            });

        var dumpBody = JsonSerializer.Serialize(new
        {
            category = "dump/layout",
            summary = "BoxTree nodes=3",
            nodeCount = 3,
            nodes = new[]
            {
                new
                {
                    id = "layout.0",
                    type = "BlockBox",
                    name = "p",
                    attributes = new
                    {
                        childCount = 1,
                        text = "Hello Html2x"
                    },
                    children = Array.Empty<object>()
                }
            }
        });

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            sessionDescriptor.SessionId,
            "dump/layout",
            "dump/layout",
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["summary"] = "BoxTree nodes=3",
                ["nodeCount"] = 3,
                ["reason"] = "Regression capture"
            },
            new StructuredDumpMetadata("json", "BoxTree nodes=3", 3, dumpBody));

        var contexts = new List<DiagnosticContextSnapshot>
        {
            new(
                Guid.NewGuid(),
                sessionDescriptor.SessionId,
                "ShrinkToFit",
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["availableWidth"] = 140,
                    ["intrinsicWidth"] = 210
                },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddMilliseconds(10))
        };

        var model = new DiagnosticsModel(sessionDescriptor, diagnosticEvent, contexts);

        sink.Publish(model);

        Assert.True(File.Exists(outputPath), "JSON sink did not create an output file.");

        using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var entry = root[0];
        Assert.True(entry.TryGetProperty("session", out var sessionElement));
        Assert.Equal(sessionDescriptor.SessionId.ToString(), sessionElement.GetProperty("sessionId").GetString());
        Assert.Equal("alpha", sessionElement.GetProperty("metadata").GetProperty("tenant").GetString());

        Assert.True(entry.TryGetProperty("event", out var eventElement));
        Assert.Equal("dump/layout", eventElement.GetProperty("category").GetString());
        Assert.Equal("Regression capture", eventElement.GetProperty("payload").GetProperty("reason").GetString());

        var dumpElement = eventElement.GetProperty("dump");
        Assert.Equal("json", dumpElement.GetProperty("format").GetString());
        Assert.Equal(3, dumpElement.GetProperty("nodeCount").GetInt32());
        Assert.Equal(JsonValueKind.Object, dumpElement.GetProperty("body").ValueKind);

        var nodes = dumpElement.GetProperty("body").GetProperty("nodes");
        Assert.Equal(JsonValueKind.Array, nodes.ValueKind);
        Assert.Equal("layout.0", nodes[0].GetProperty("id").GetString());

        var contextsElement = entry.GetProperty("contexts");
        Assert.Equal(1, contextsElement.GetArrayLength());
        Assert.Equal("ShrinkToFit", contextsElement[0].GetProperty("name").GetString());
        Assert.Equal(140, contextsElement[0].GetProperty("values").GetProperty("availableWidth").GetInt32());
    }

    private static IDiagnosticSink CreateJsonSinkOrFail(string outputPath)
    {
        var sinkType = Type.GetType(JsonSinkType);
        Assert.NotNull(sinkType);

        var optionsType = Type.GetType(JsonSinkOptionsType);
        Assert.NotNull(optionsType);

        object? optionsInstance;
        try
        {
            optionsInstance = Activator.CreateInstance(optionsType!, outputPath);
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("JsonDiagnosticSinkOptions must expose a public constructor accepting a file path.");
        }

        Assert.NotNull(optionsInstance);

        object? sinkInstance;
        try
        {
            sinkInstance = Activator.CreateInstance(sinkType!, optionsInstance);
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException("JsonDiagnosticSink must expose a public constructor accepting JsonDiagnosticSinkOptions.");
        }

        return Assert.IsAssignableFrom<IDiagnosticSink>(sinkInstance);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "json-sink-test-" + Guid.NewGuid());
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in tests.
            }
        }
    }
}
