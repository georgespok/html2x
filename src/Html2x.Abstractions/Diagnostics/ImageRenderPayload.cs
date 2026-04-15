using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.Abstractions.Diagnostics;

/// <summary>
/// Diagnostics payload describing a single image render operation.
/// </summary>
public sealed class ImageRenderPayload : IDiagnosticsPayload
{
    public string Kind => "image.render";

    public string Src { get; init; } = string.Empty;

    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Info;

    public DiagnosticContext? Context { get; init; }

    public SizePt RenderedSize { get; init; }

    public ImageStatus Status { get; init; }

    public BorderEdges? Borders { get; init; }
}
