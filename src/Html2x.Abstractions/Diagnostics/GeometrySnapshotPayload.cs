namespace Html2x.Abstractions.Diagnostics;

public sealed class GeometrySnapshotPayload : IDiagnosticsPayload
{
    public string Kind => "layout.geometry";

    public GeometrySnapshot Snapshot { get; init; } = null!;
}
