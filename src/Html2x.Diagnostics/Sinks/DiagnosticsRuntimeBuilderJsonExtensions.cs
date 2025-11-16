using Html2x.Diagnostics.Runtime;

namespace Html2x.Diagnostics.Sinks;

public static class DiagnosticsRuntimeBuilderJsonExtensions
{
    private static readonly string JsonSinkType = typeof(JsonDiagnosticSink).FullName
        ?? "Html2x.Diagnostics.Sinks.JsonDiagnosticSink";

    public static DiagnosticsRuntimeBuilder AddJsonSink(
        this DiagnosticsRuntimeBuilder builder,
        string outputPath)
    {
        return AddJsonSink(builder, "json-file", outputPath, configure: null);
    }

    public static DiagnosticsRuntimeBuilder AddJsonSink(
        this DiagnosticsRuntimeBuilder builder,
        string sinkId,
        string outputPath,
        Action<JsonDiagnosticSinkOptions>? configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var options = new JsonDiagnosticSinkOptions(sinkId, outputPath);
        configure?.Invoke(options);

        var configuration = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["outputPath"] = options.OutputPath,
            ["append"] = options.Append,
            ["writeIndented"] = options.WriteIndented
        };

        return builder.AddSink(
            sinkId,
            JsonSinkType,
            () => new JsonDiagnosticSink(options.Clone()),
            configuration);
    }
}
