using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.Abstractions.Diagnostics;

public sealed class MarginCollapsePayload : IDiagnosticsPayload
{
    public string Kind => "layout.margin-collapse";
    public float PreviousBottomMargin { get; init; }
    public float NextTopMargin { get; init; }
    public float CollapsedTopMargin { get; init; }
    public string? Owner { get; init; }
    public string? Consumer { get; init; }
    public FormattingContextKind? FormattingContext { get; init; }
}
