using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics;

public sealed class HtmlPayload : IDiagnosticsPayload
{
    public string Kind => "html";
    public string Html { get; init; }

    /// <summary>Status of the most recent image processed (ok, missing, oversize).</summary>
    public ImageStatus? ImageStatus { get; init; }

    /// <summary>Applied scale factor for the rendered image, if any (1.0 means no scale).</summary>
    public double? AppliedImageScale { get; init; }

    /// <summary>Optional warning related to image processing (e.g., oversize rejection).</summary>
    public string? ImageWarning { get; init; }
}