using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine.Diagnostics;

internal static class GeometrySnapshotDiagnostics
{
    private const string EventName = "layout/geometry-snapshot";

    private const string SnapshotFieldName = "snapshot";

    public static void Emit(
        PublishedLayoutTree publishedLayout,
        PaginationResult pagination,
        IDiagnosticsSink? diagnosticsSink)
    {
        diagnosticsSink?.Emit(new(
            LayoutStageNames.Pagination,
            EventName,
            DiagnosticSeverity.Info,
            null,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(
                    SnapshotFieldName,
                    GeometrySnapshotMapper.ToDiagnosticObject(publishedLayout, pagination))),
            DateTimeOffset.UtcNow));
    }
}