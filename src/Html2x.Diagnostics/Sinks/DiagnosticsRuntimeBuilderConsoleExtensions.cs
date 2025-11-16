using Html2x.Diagnostics.Runtime;

namespace Html2x.Diagnostics.Sinks;

public static class DiagnosticsRuntimeBuilderConsoleExtensions
{
    private static readonly string ConsoleSinkType = typeof(ConsoleDiagnosticSink).FullName
        ?? "Html2x.Diagnostics.Sinks.ConsoleDiagnosticSink";

    public static DiagnosticsRuntimeBuilder AddConsoleSink(
        this DiagnosticsRuntimeBuilder builder)
    {
        return AddConsoleSink(builder, Console.Out, configure: null);
    }

    public static DiagnosticsRuntimeBuilder AddConsoleSink(
        this DiagnosticsRuntimeBuilder builder,
        TextWriter writer,
        Action<ConsoleDiagnosticSinkOptions>? configure = null)
    {
        return AddConsoleSink(builder, "console-sink", writer, configure);
    }

    public static DiagnosticsRuntimeBuilder AddConsoleSink(
        this DiagnosticsRuntimeBuilder builder,
        string sinkId,
        TextWriter writer,
        Action<ConsoleDiagnosticSinkOptions>? configure = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var options = new ConsoleDiagnosticSinkOptions(sinkId, writer);
        configure?.Invoke(options);

        var configuration = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["includeTimestamp"] = options.IncludeTimestamp,
            ["includeSessionName"] = options.IncludeSessionName
        };

        return builder.AddSink(
            sinkId,
            ConsoleSinkType,
            () => new ConsoleDiagnosticSink(options.Clone()),
            configuration);
    }
}
