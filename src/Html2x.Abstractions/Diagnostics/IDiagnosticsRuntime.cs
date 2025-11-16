using Html2x.Abstractions.Diagnostics.Contracts;

namespace Html2x.Abstractions.Diagnostics;

public interface IDiagnosticsRuntime
{
    bool DiagnosticsEnabled { get; }

    DiagnosticSessionConfiguration DefaultConfiguration { get; }

    IDiagnosticSession StartSession(
        string name,
        DiagnosticSessionConfiguration? configurationOverride = null,
        IReadOnlyDictionary<string, object?>? metadata = null);

    T Decorate<T>(T component) where T : class;
}

public interface IDiagnosticSession : IDisposable
{
    DiagnosticSessionDescriptor Descriptor { get; }

    bool IsEnabled { get; }

    IDiagnosticContextScope Context(
        string name,
        IReadOnlyDictionary<string, object?>? seedValues = null);

    void Publish(DiagnosticEvent diagnosticEvent);
}

public interface IDiagnosticContextScope : IDisposable
{
    DiagnosticContextSnapshot Snapshot { get; }

    void Set(string key, object? value);
}

public interface IDiagnosticSink
{
    string SinkId { get; }

    void Publish(DiagnosticsModel model);
}
