using Html2x.Diagnostics.Runtime;

namespace Html2x.Diagnostics.Sinks;

public static class DiagnosticsRuntimeBuilderInMemoryExtensions
{
    private static readonly string InMemorySinkType = typeof(InMemoryDiagnosticSink).FullName
        ?? "Html2x.Diagnostics.Sinks.InMemoryDiagnosticSink";

    public static DiagnosticsRuntimeBuilder AddInMemorySink(
        this DiagnosticsRuntimeBuilder builder,
        string sinkId = "in-memory-sink",
        int capacity = 1024,
        Action<InMemoryDiagnosticSink>? configure = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        InMemoryDiagnosticSink? instance = null;

        return builder.AddSink(
            sinkId,
            InMemorySinkType,
            () =>
            {
                instance ??= new InMemoryDiagnosticSink(sinkId, capacity);
                configure?.Invoke(instance);
                return instance;
            },
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["capacity"] = capacity
            });
    }
}
