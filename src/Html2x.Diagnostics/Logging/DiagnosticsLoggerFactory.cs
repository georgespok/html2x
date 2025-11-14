using Html2x.Abstractions.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Html2x.Diagnostics.Logging;

public sealed class DiagnosticsLoggerFactory(
    Func<IDiagnosticSession?> sessionAccessor,
    DiagnosticsLoggerOptions? options = null)
    : ILoggerFactory
{
    private readonly Func<IDiagnosticSession?> _sessionAccessor = sessionAccessor ?? throw new ArgumentNullException(nameof(sessionAccessor));
    private readonly DiagnosticsLoggerOptions _options = options ?? new DiagnosticsLoggerOptions();

    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticsLogger(categoryName, _sessionAccessor, _options.MinimumLevel);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotSupportedException("DiagnosticsLoggerFactory does not support external providers.");
    }

    public void Dispose()
    {
        // Nothing to dispose at the moment.
    }
}
