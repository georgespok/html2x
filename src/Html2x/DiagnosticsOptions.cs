using Html2x.Diagnostics.Runtime;
using Html2x.Diagnostics.Sinks;

namespace Html2x;

public sealed class DiagnosticsOptions
{
    public bool EnableConsoleSink { get; set; } = true;

    public bool EnableJsonSink => JsonOutputPath is not null;

    public string? JsonOutputPath { get; set; }

    public bool EnableInMemorySink { get; set; }

    public int InMemoryCapacity { get; set; } = 1024;

    public DiagnosticsRuntime BuildRuntime()
    {
        return DiagnosticsRuntime.Configure(builder =>
        {
            if (EnableConsoleSink)
            {
                builder.AddConsoleSink(Console.Out);
            }

            if (EnableJsonSink)
            {
                var resolved = Path.GetFullPath(JsonOutputPath!);
                builder.AddJsonSink(resolved);
            }

            if (EnableInMemorySink)
            {
                builder.AddInMemorySink(capacity: InMemoryCapacity);
            }
        });
    }
}
