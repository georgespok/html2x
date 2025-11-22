using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics
{
    public sealed class LayoutSnapshotPayload : IDiagnosticsPayload
    {
        public string Kind => "layout.snapshot";
        public LayoutSnapshot Snapshot { get; init; } = null!;
    }
}
