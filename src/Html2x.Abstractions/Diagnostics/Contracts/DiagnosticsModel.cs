namespace Html2x.Abstractions.Diagnostics.Contracts;

public sealed record DiagnosticsModel
{
    public DiagnosticsModel(
        DiagnosticSessionDescriptor session,
        DiagnosticEvent diagnosticEvent,
        IReadOnlyList<DiagnosticContextSnapshot> activeContexts)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Event = diagnosticEvent ?? throw new ArgumentNullException(nameof(diagnosticEvent));
        ActiveContexts = activeContexts ?? [];
    }

    public DiagnosticSessionDescriptor Session { get; }

    public DiagnosticEvent Event { get; }

    public IReadOnlyList<DiagnosticContextSnapshot> ActiveContexts { get; }
}
