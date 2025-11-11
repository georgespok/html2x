using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Html2x.Test;

internal sealed class TestOutputLoggerProvider(ITestOutputHelper output, LogLevel minLevel = LogLevel.Trace)
    : ILoggerProvider, ISupportExternalScope
{
    private readonly ITestOutputHelper _output = output ?? throw new ArgumentNullException(nameof(output));
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(
            _output,
            categoryName,
            minLevel,
            () => _scopeProvider);
    }

    public void Dispose()
    {
        // no-op
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
    }

    private sealed class TestOutputLogger(
        ITestOutputHelper output,
        string categoryName,
        LogLevel minLevel,
        Func<IExternalScopeProvider> scopeProviderAccessor)
        : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            var provider = scopeProviderAccessor();
            return provider.Push(state!);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= minLevel && logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var scopeProvider = scopeProviderAccessor();
            var message = formatter?.Invoke(state, exception) ?? state?.ToString() ?? string.Empty;

            var builder = new StringBuilder();
            builder
                .Append(categoryName)
                .Append(" ")
                .Append(logLevel)
                .Append(": ")
                .Append(message);

            scopeProvider.ForEachScope((scope, sb) =>
            {
                sb.Append(" | scope: ").Append(scope);
            }, builder);

            if (exception is not null)
            {
                builder.AppendLine().Append(exception);
            }

            output.WriteLine(builder.ToString());
        }
    }
}
