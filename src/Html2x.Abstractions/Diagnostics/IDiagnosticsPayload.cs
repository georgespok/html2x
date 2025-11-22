namespace Html2x.Abstractions.Diagnostics;

public interface IDiagnosticsPayload
{
    string Kind { get; } // e.g. "layout.snapshot", "render.summary"
}