using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;

namespace Html2x.TestConsole.Diagnostics;

internal sealed class ConsoleDiagnosticSink : IDiagnosticSink
{
    private readonly ILogger _logger;

    public ConsoleDiagnosticSink(ILogger logger)
    {
        _logger = logger;
    }

    public string SinkId => "console-sink";

    public void Publish(DiagnosticsModel model)
    {
        _logger.LogInformation("[Diagnostics] {Category} {Kind} {Timestamp}", model.Event.Category, model.Event.Kind, model.Event.Timestamp);
    }
}
