namespace Html2x.Abstractions.Diagnostics;

public sealed class MarginCollapsePayload : IDiagnosticsPayload
{
    public string Kind => "layout.margin-collapse";
    public float PreviousBottomMargin { get; init; }
    public float NextTopMargin { get; init; }
    public float CollapsedTopMargin { get; init; }
}
