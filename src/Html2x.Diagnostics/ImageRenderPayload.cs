using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics;

/// <summary>
/// Diagnostics payload describing a single image render operation.
/// </summary>
public sealed class ImageRenderPayload : IDiagnosticsPayload
{
    public string Kind => "image.render";

    public string Src { get; init; } = string.Empty;

    public double RenderedWidth { get; init; }

    public double RenderedHeight { get; init; }

    public ImageStatus Status { get; init; }
}
