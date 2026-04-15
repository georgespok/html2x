namespace Html2x.Abstractions.Diagnostics;

public sealed class PaginationTracePayload : IDiagnosticsPayload
{
    public string Kind => "layout.pagination.trace";

    public string EventName { get; init; } = string.Empty;

    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Info;

    public DiagnosticContext? Context { get; init; }

    public int PageNumber { get; init; }

    public int? FragmentId { get; init; }

    public int? FromPage { get; init; }

    public int? ToPage { get; init; }

    public float? LocalY { get; init; }

    public float? RemainingSpace { get; init; }

    public float? RemainingSpaceBefore { get; init; }

    public float? RemainingSpaceAfter { get; init; }

    public float? BlockHeight { get; init; }

    public float? PageContentHeight { get; init; }

    public string? Reason { get; init; }
}
