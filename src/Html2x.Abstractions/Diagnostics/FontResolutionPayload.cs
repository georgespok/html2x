using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Abstractions.Diagnostics;

public sealed class FontResolutionPayload : IDiagnosticsPayload
{
    public string Kind => "font.resolution";

    public string Owner { get; init; } = string.Empty;

    public string Consumer { get; init; } = string.Empty;

    public string RequestedFamily { get; init; } = string.Empty;

    public FontWeight RequestedWeight { get; init; }

    public FontStyle RequestedStyle { get; init; }

    public string? ResolvedFamily { get; init; }

    public FontWeight? ResolvedWeight { get; init; }

    public FontStyle? ResolvedStyle { get; init; }

    public string? SourceId { get; init; }

    public string? ConfiguredPath { get; init; }

    public string? FilePath { get; init; }

    public int? FaceIndex { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public string? Reason { get; init; }
}
