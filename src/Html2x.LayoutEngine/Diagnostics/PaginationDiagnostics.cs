using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Diagnostics;

public static class PaginationDiagnostics
{
    private const string PageCreatedEvent = "layout/pagination/page-created";
    private const string BlockMovedNextPageEvent = "layout/pagination/block-moved-next-page";
    private const string BlockPlacedEvent = "layout/pagination/block-placed";
    private const string OversizedBlockEvent = "layout/pagination/oversized-block";
    private const string EmptyDocumentEvent = "layout/pagination/empty-document";

    public static void EmitPageCreated(
        IDiagnosticsSink? diagnosticsSink,
        int pageNumber,
        string reason)
    {
        Emit(
            diagnosticsSink,
            PageCreatedEvent,
            new PaginationDiagnostic(PageCreatedEvent, DiagnosticSeverity.Info, CreateContext(pageNumber, null), pageNumber)
            {
                Reason = reason
            });
    }

    public static void EmitBlockMovedNextPage(
        IDiagnosticsSink? diagnosticsSink,
        int fromPage,
        int toPage,
        int fragmentId,
        float remainingSpace,
        float blockHeight)
    {
        Emit(
            diagnosticsSink,
            BlockMovedNextPageEvent,
            new PaginationDiagnostic(BlockMovedNextPageEvent, DiagnosticSeverity.Info, CreateContext(toPage, fragmentId), toPage)
            {
                FromPage = fromPage,
                ToPage = toPage,
                FragmentId = fragmentId,
                RemainingSpace = remainingSpace,
                BlockHeight = blockHeight
            });
    }

    public static void EmitBlockPlaced(
        IDiagnosticsSink? diagnosticsSink,
        int pageNumber,
        int fragmentId,
        float localY,
        float blockHeight,
        float remainingSpaceBefore,
        float remainingSpaceAfter)
    {
        Emit(
            diagnosticsSink,
            BlockPlacedEvent,
            new PaginationDiagnostic(BlockPlacedEvent, DiagnosticSeverity.Info, CreateContext(pageNumber, fragmentId), pageNumber)
            {
                FragmentId = fragmentId,
                LocalY = localY,
                BlockHeight = blockHeight,
                RemainingSpaceBefore = remainingSpaceBefore,
                RemainingSpaceAfter = remainingSpaceAfter
            });
    }

    public static void EmitOversizedBlock(
        IDiagnosticsSink? diagnosticsSink,
        int pageNumber,
        int fragmentId,
        float blockHeight,
        float pageContentHeight)
    {
        Emit(
            diagnosticsSink,
            OversizedBlockEvent,
            new PaginationDiagnostic(OversizedBlockEvent, DiagnosticSeverity.Warning, CreateContext(pageNumber, fragmentId), pageNumber)
            {
                FragmentId = fragmentId,
                BlockHeight = blockHeight,
                PageContentHeight = pageContentHeight
            });
    }

    public static void EmitEmptyDocument(
        IDiagnosticsSink? diagnosticsSink,
        int pageNumber)
    {
        Emit(
            diagnosticsSink,
            EmptyDocumentEvent,
            new PaginationDiagnostic(EmptyDocumentEvent, DiagnosticSeverity.Info, CreateContext(pageNumber, null), pageNumber));
    }

    private static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        string eventName,
        PaginationDiagnostic payload)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/pagination",
            Name: eventName,
            Severity: payload.Severity,
            Message: payload.Reason,
            Context: payload.Context,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("eventName", payload.EventName),
                DiagnosticFields.Field("pageNumber", payload.PageNumber),
                DiagnosticFields.Field("fragmentId", FromNullable(payload.FragmentId)),
                DiagnosticFields.Field("fromPage", FromNullable(payload.FromPage)),
                DiagnosticFields.Field("toPage", FromNullable(payload.ToPage)),
                DiagnosticFields.Field("localY", FromNullable(payload.LocalY)),
                DiagnosticFields.Field("remainingSpace", FromNullable(payload.RemainingSpace)),
                DiagnosticFields.Field("remainingSpaceBefore", FromNullable(payload.RemainingSpaceBefore)),
                DiagnosticFields.Field("remainingSpaceAfter", FromNullable(payload.RemainingSpaceAfter)),
                DiagnosticFields.Field("blockHeight", FromNullable(payload.BlockHeight)),
                DiagnosticFields.Field("pageContentHeight", FromNullable(payload.PageContentHeight)),
                DiagnosticFields.Field("reason", payload.Reason is null ? null : DiagnosticValue.From(payload.Reason))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static DiagnosticValue? FromNullable(int? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticValue? FromNullable(float? value) =>
        value.HasValue ? DiagnosticValue.From(value.Value) : null;

    private static DiagnosticContext CreateContext(int pageNumber, int? fragmentId)
    {
        var structuralPath = fragmentId.HasValue
            ? $"page[{pageNumber}]/fragment[{fragmentId.Value}]"
            : $"page[{pageNumber}]";

        return new DiagnosticContext(
            Selector: null,
            ElementIdentity: fragmentId.HasValue ? $"fragment#{fragmentId.Value}" : null,
            StyleDeclaration: null,
            StructuralPath: structuralPath,
            RawUserInput: null);
    }

    private sealed record PaginationDiagnostic(
        string EventName,
        DiagnosticSeverity Severity,
        DiagnosticContext Context,
        int PageNumber)
    {
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

}
