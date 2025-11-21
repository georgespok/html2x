using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics;

public sealed class HtmlPayload : IDiagnosticsPayload
{
    public string Kind => "html";
    public string Html { get; init; }
}