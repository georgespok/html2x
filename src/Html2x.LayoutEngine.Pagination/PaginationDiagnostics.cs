using Html2x.Diagnostics.Contracts;

namespace Html2x.LayoutEngine.Pagination;

internal static class PaginationDiagnostics
{
    public static void EmitPageCreated(
        IDiagnosticsSink? diagnosticsSink,
        int pageNumber,
        string reason)
    {
        Emit(
            diagnosticsSink,
            PaginationDiagnosticNames.Events.PageCreated,
            new PaginationDiagnostic(
                PaginationDiagnosticNames.Events.PageCreated,
                DiagnosticSeverity.Info,
                CreateContext(pageNumber, null),
                pageNumber)
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
            PaginationDiagnosticNames.Events.BlockMovedNextPage,
            new PaginationDiagnostic(
                PaginationDiagnosticNames.Events.BlockMovedNextPage,
                DiagnosticSeverity.Info,
                CreateContext(toPage, fragmentId),
                toPage)
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
            PaginationDiagnosticNames.Events.BlockPlaced,
            new PaginationDiagnostic(
                PaginationDiagnosticNames.Events.BlockPlaced,
                DiagnosticSeverity.Info,
                CreateContext(pageNumber, fragmentId),
                pageNumber)
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
            PaginationDiagnosticNames.Events.OversizedBlock,
            new PaginationDiagnostic(
                PaginationDiagnosticNames.Events.OversizedBlock,
                DiagnosticSeverity.Warning,
                CreateContext(pageNumber, fragmentId),
                pageNumber)
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
            PaginationDiagnosticNames.Events.EmptyDocument,
            new PaginationDiagnostic(
                PaginationDiagnosticNames.Events.EmptyDocument,
                DiagnosticSeverity.Info,
                CreateContext(pageNumber, null),
                pageNumber));
    }

    private static void Emit(
        IDiagnosticsSink? diagnosticsSink,
        string eventName,
        PaginationDiagnostic payload)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: PaginationDiagnosticNames.Stages.Pagination,
            Name: eventName,
            Severity: payload.Severity,
            Message: payload.Reason,
            Context: payload.Context,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.EventName, payload.EventName),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.PageNumber, payload.PageNumber),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.FragmentId, FromNullable(payload.FragmentId)),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.FromPage, FromNullable(payload.FromPage)),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.ToPage, FromNullable(payload.ToPage)),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.LocalY, FromNullable(payload.LocalY)),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.RemainingSpace, FromNullable(payload.RemainingSpace)),
                DiagnosticFields.Field(
                    PaginationDiagnosticNames.Fields.RemainingSpaceBefore,
                    FromNullable(payload.RemainingSpaceBefore)),
                DiagnosticFields.Field(
                    PaginationDiagnosticNames.Fields.RemainingSpaceAfter,
                    FromNullable(payload.RemainingSpaceAfter)),
                DiagnosticFields.Field(PaginationDiagnosticNames.Fields.BlockHeight, FromNullable(payload.BlockHeight)),
                DiagnosticFields.Field(
                    PaginationDiagnosticNames.Fields.PageContentHeight,
                    FromNullable(payload.PageContentHeight)),
                DiagnosticFields.Field(
                    PaginationDiagnosticNames.Fields.Reason,
                    payload.Reason is null ? null : DiagnosticValue.From(payload.Reason))),
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

internal static class PaginationDiagnosticNames
{
    public static class Stages
    {
        public const string Pagination = "stage/pagination";
    }

    public static class Events
    {
        public const string PageCreated = "layout/pagination/page-created";
        public const string BlockMovedNextPage = "layout/pagination/block-moved-next-page";
        public const string BlockPlaced = "layout/pagination/block-placed";
        public const string OversizedBlock = "layout/pagination/oversized-block";
        public const string EmptyDocument = "layout/pagination/empty-document";
    }

    public static class Fields
    {
        public const string EventName = "eventName";
        public const string PageNumber = "pageNumber";
        public const string FragmentId = "fragmentId";
        public const string FromPage = "fromPage";
        public const string ToPage = "toPage";
        public const string LocalY = "localY";
        public const string RemainingSpace = "remainingSpace";
        public const string RemainingSpaceBefore = "remainingSpaceBefore";
        public const string RemainingSpaceAfter = "remainingSpaceAfter";
        public const string BlockHeight = "blockHeight";
        public const string PageContentHeight = "pageContentHeight";
        public const string Reason = "reason";
    }

    public static class Reasons
    {
        public const string InitialPage = "InitialPage";
        public const string Overflow = "Overflow";
    }
}
