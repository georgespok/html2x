namespace Html2x.Abstractions.Diagnostics;

public sealed class TableLayoutPayload : IDiagnosticsPayload
{
    public string Kind => "layout.table";

    public string NodePath { get; init; } = string.Empty;

    public int RowCount { get; init; }

    public int? DerivedColumnCount { get; init; }

    public float? RequestedWidth { get; init; }

    public float? ResolvedWidth { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public string? Reason { get; init; }
}
