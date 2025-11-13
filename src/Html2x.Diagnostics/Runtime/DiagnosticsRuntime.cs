using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Logging;
using Html2x.Diagnostics.Pipeline;

namespace Html2x.Diagnostics.Runtime;

public sealed class DiagnosticsRuntime : IDiagnosticsRuntime
{
    private readonly IReadOnlyDictionary<string, DiagnosticsRuntimeBuilder.SinkRegistration> _sinkMap;
    private readonly DiagnosticSessionConfiguration _defaultConfiguration;
    private readonly DiagnosticsLoggerFactory _loggerFactory;
    private readonly AsyncLocal<IDiagnosticSession?> _currentSession = new();

    private DiagnosticsRuntime(
        IReadOnlyDictionary<string, DiagnosticsRuntimeBuilder.SinkRegistration> sinkMap,
        DiagnosticSessionConfiguration defaultConfiguration,
        DiagnosticsLoggerOptions loggerOptions)
    {
        _sinkMap = sinkMap;
        _defaultConfiguration = defaultConfiguration;
        _loggerFactory = new DiagnosticsLoggerFactory(GetCurrentSession, loggerOptions);
    }

    public static DiagnosticsRuntime Configure(Action<DiagnosticsRuntimeBuilder>? configure = null)
    {
        var builder = new DiagnosticsRuntimeBuilder();
        configure?.Invoke(builder);

        var registrations = builder.BuildRegistrations();
        var sinkMap = new Dictionary<string, DiagnosticsRuntimeBuilder.SinkRegistration>(
            StringComparer.OrdinalIgnoreCase);
        var descriptors = new List<DiagnosticSinkDescriptor>(registrations.Count);

        foreach (var registration in registrations)
        {
            sinkMap[registration.Descriptor.SinkId] = registration;
            descriptors.Add(registration.Descriptor);
        }

        var configuration = new DiagnosticSessionConfiguration(
            descriptors,
            builder.PropagateSinkExceptions);

        return new DiagnosticsRuntime(sinkMap, configuration, builder.Logger);
    }

    public bool DiagnosticsEnabled => _defaultConfiguration.Sinks.Count > 0;

    public DiagnosticSessionConfiguration DefaultConfiguration => _defaultConfiguration;

    public IDiagnosticSession StartSession(
        string name,
        DiagnosticSessionConfiguration? configurationOverride = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Session name is required.", nameof(name));
        }

        if (_currentSession.Value is not null)
        {
            throw new InvalidOperationException("A diagnostics session is already active on this context.");
        }

        var configuration = configurationOverride ?? _defaultConfiguration;
        var sinks = InstantiateSinks(configuration);

        var descriptor = new DiagnosticSessionDescriptor(
            Guid.NewGuid(),
            name,
            DateTimeOffset.UtcNow,
            sinks.Count > 0,
            configuration,
            metadata ?? new Dictionary<string, object?>(StringComparer.Ordinal));

        var dispatcher = new DiagnosticsDispatcher(sinks, configuration.PropagateSinkExceptions);
        var session = new DiagnosticSession(descriptor, dispatcher, ClearSession);

        _currentSession.Value = session;
        return session;
    }

    public T Decorate<T>(T component) where T : class
    {
        if (component is IDiagnosticsHost host)
        {
            host.AttachDiagnosticsSession(GetCurrentSession);
        }

        return component;
    }

    private IDiagnosticSession? GetCurrentSession() => _currentSession.Value;

    private void ClearSession(IDiagnosticSession session)
    {
        if (ReferenceEquals(_currentSession.Value, session))
        {
            _currentSession.Value = null;
        }
    }

    private IReadOnlyList<IDiagnosticSink> InstantiateSinks(DiagnosticSessionConfiguration configuration)
    {
        var sinks = new List<IDiagnosticSink>(configuration.Sinks.Count);

        foreach (var descriptor in configuration.Sinks)
        {
            if (!_sinkMap.TryGetValue(descriptor.SinkId, out var registration))
            {
                throw new InvalidOperationException($"Unknown sink '{descriptor.SinkId}'.");
            }

            sinks.Add(registration.Factory());
        }

        return sinks;
    }
}
