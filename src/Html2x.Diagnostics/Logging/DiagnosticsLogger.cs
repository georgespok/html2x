using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;

namespace Html2x.Diagnostics.Logging;

internal sealed class DiagnosticsLogger(
    string categoryName,
    Func<IDiagnosticSession?> sessionAccessor,
    LogLevel minimumLevel)
    : ILogger
{
    private readonly string _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
    private readonly Func<IDiagnosticSession?> _sessionAccessor = sessionAccessor ?? throw new ArgumentNullException(nameof(sessionAccessor));

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || formatter is null)
        {
            return;
        }

        var session = _sessionAccessor();
        if (session is null || !session.IsEnabled)
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["message"] = formatter(state, exception),
            ["eventId"] = eventId.Id,
            ["eventName"] = eventId.Name,
            ["state"] = state?.ToString()
        };

        if (exception is not null)
        {
            payload["exception"] = exception.ToString();
        }

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            session.Descriptor.SessionId,
            $"log/{_categoryName}",
            $"log/{logLevel.ToString().ToLowerInvariant()}",
            DateTimeOffset.UtcNow,
            payload);

        session.Publish(diagnosticEvent);
    }
}
