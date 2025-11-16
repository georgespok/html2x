using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Diagnostics.Pipeline;

internal sealed class DiagnosticsDispatcher(
    IReadOnlyList<IDiagnosticSink> sinks,
    bool propagateSinkExceptions)
{
    private readonly IReadOnlyList<IDiagnosticSink> _sinks = sinks ?? [];

    public bool HasSinks => _sinks.Count > 0;

    public void Dispatch(DiagnosticsModel model)
    {
        if (!HasSinks)
        {
            return;
        }

        foreach (var sink in _sinks)
        {
            try
            {
                sink.Publish(model);
            }
            catch when (!propagateSinkExceptions)
            {
                // Swallow sink failure; remaining sinks continue.
            }
        }
    }
}
