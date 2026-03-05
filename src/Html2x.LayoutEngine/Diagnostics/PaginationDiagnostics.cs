using Html2x.Abstractions.Diagnostics;

namespace Html2x.LayoutEngine.Diagnostics;

public static class PaginationDiagnostics
{
    private const string PageCreatedEvent = "layout/pagination/page-created";
    private const string BlockMovedNextPageEvent = "layout/pagination/block-moved-next-page";
    private const string BlockPlacedEvent = "layout/pagination/block-placed";
    private const string OversizedBlockEvent = "layout/pagination/oversized-block";
    private const string EmptyDocumentEvent = "layout/pagination/empty-document";

    public static void EmitPageCreated(DiagnosticsSession? diagnosticsSession, int pageNumber, string reason)
    {
        Emit(
            diagnosticsSession,
            PageCreatedEvent,
            new PaginationTracePayload
            {
                EventName = PageCreatedEvent,
                PageNumber = pageNumber,
                Reason = reason
            });
    }

    public static void EmitBlockMovedNextPage(
        DiagnosticsSession? diagnosticsSession,
        int fromPage,
        int toPage,
        int fragmentId,
        float remainingSpace,
        float blockHeight)
    {
        Emit(
            diagnosticsSession,
            BlockMovedNextPageEvent,
            new PaginationTracePayload
            {
                EventName = BlockMovedNextPageEvent,
                PageNumber = toPage,
                FromPage = fromPage,
                ToPage = toPage,
                FragmentId = fragmentId,
                RemainingSpace = remainingSpace,
                BlockHeight = blockHeight
            });
    }

    public static void EmitBlockPlaced(
        DiagnosticsSession? diagnosticsSession,
        int pageNumber,
        int fragmentId,
        float localY,
        float blockHeight,
        float remainingSpaceBefore,
        float remainingSpaceAfter)
    {
        Emit(
            diagnosticsSession,
            BlockPlacedEvent,
            new PaginationTracePayload
            {
                EventName = BlockPlacedEvent,
                PageNumber = pageNumber,
                FragmentId = fragmentId,
                LocalY = localY,
                BlockHeight = blockHeight,
                RemainingSpaceBefore = remainingSpaceBefore,
                RemainingSpaceAfter = remainingSpaceAfter
            });
    }

    public static void EmitOversizedBlock(
        DiagnosticsSession? diagnosticsSession,
        int pageNumber,
        int fragmentId,
        float blockHeight,
        float pageContentHeight)
    {
        Emit(
            diagnosticsSession,
            OversizedBlockEvent,
            new PaginationTracePayload
            {
                EventName = OversizedBlockEvent,
                PageNumber = pageNumber,
                FragmentId = fragmentId,
                BlockHeight = blockHeight,
                PageContentHeight = pageContentHeight
            });
    }

    public static void EmitEmptyDocument(DiagnosticsSession? diagnosticsSession, int pageNumber)
    {
        Emit(
            diagnosticsSession,
            EmptyDocumentEvent,
            new PaginationTracePayload
            {
                EventName = EmptyDocumentEvent,
                PageNumber = pageNumber
            });
    }

    private static void Emit(DiagnosticsSession? diagnosticsSession, string eventName, IDiagnosticsPayload payload)
    {
        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = eventName,
            Payload = payload
        });
    }

}
