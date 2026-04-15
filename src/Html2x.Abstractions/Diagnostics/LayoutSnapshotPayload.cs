namespace Html2x.Abstractions.Diagnostics;

public sealed class LayoutSnapshotPayload : IDiagnosticsPayload
{
    public string Kind => "layout.snapshot";

    public LayoutSnapshot Snapshot { get; init; } = null!;
}
