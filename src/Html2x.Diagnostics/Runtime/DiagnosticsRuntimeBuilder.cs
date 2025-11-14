using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Diagnostics.Logging;

namespace Html2x.Diagnostics.Runtime;

public sealed class DiagnosticsRuntimeBuilder
{
    private readonly Dictionary<string, SinkRegistration> _sinks =
        new(StringComparer.OrdinalIgnoreCase);

    public DiagnosticsLoggerOptions Logger { get; } = new();

    public bool PropagateSinkExceptions { get; private set; } = true;

    public DiagnosticsRuntimeBuilder AddSink(
        string sinkId,
        string sinkType,
        Func<IDiagnosticSink> sinkFactory,
        IReadOnlyDictionary<string, object?>? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(sinkId))
        {
            throw new ArgumentException("Sink identifier is required.", nameof(sinkId));
        }

        if (sinkFactory is null)
        {
            throw new ArgumentNullException(nameof(sinkFactory));
        }

        var descriptor = new DiagnosticSinkDescriptor(
            sinkId,
            sinkType,
            DiagnosticSinkStatus.Active,
            configuration ?? new Dictionary<string, object?>(StringComparer.Ordinal));

        _sinks[sinkId] = new SinkRegistration(descriptor, sinkFactory);
        return this;
    }

    public DiagnosticsRuntimeBuilder SetSinkExceptionPropagation(bool propagate)
    {
        PropagateSinkExceptions = propagate;
        return this;
    }

    internal IReadOnlyList<SinkRegistration> BuildRegistrations()
    {
        return new List<SinkRegistration>(_sinks.Values);
    }

    internal sealed record SinkRegistration(
        DiagnosticSinkDescriptor Descriptor,
        Func<IDiagnosticSink> Factory);
}
