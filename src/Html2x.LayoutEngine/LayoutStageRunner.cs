using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Pagination;

namespace Html2x.LayoutEngine;

internal sealed class LayoutStageRunner
{
    private readonly FragmentBuilder _fragmentBuilder;
    private readonly LayoutGeometryBuilder _layoutGeometryBuilder;
    private readonly LayoutPaginator _layoutPaginator;

    public LayoutStageRunner(
        LayoutGeometryBuilder layoutGeometryBuilder,
        FragmentBuilder fragmentBuilder,
        LayoutPaginator layoutPaginator)
    {
        ArgumentNullException.ThrowIfNull(layoutGeometryBuilder);
        ArgumentNullException.ThrowIfNull(fragmentBuilder);
        ArgumentNullException.ThrowIfNull(layoutPaginator);

        _layoutGeometryBuilder = layoutGeometryBuilder;
        _fragmentBuilder = fragmentBuilder;
        _layoutPaginator = layoutPaginator;
    }

    public PublishedLayoutTree BuildGeometry(
        StyleTree styleTree,
        LayoutGeometryRequest request,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        return DiagnosticStage.Run(
            diagnosticsSink,
            LayoutStageNames.BoxTree,
            () => _layoutGeometryBuilder.Build(styleTree, request, diagnosticsSink),
            cancellationToken);
    }

    public FragmentTree ProjectFragments(
        PublishedLayoutTree publishedLayout,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        return DiagnosticStage.Run(
            diagnosticsSink,
            LayoutStageNames.FragmentTree,
            () => _fragmentBuilder.Build(publishedLayout),
            cancellationToken);
    }

    public PaginationResult Paginate(
        FragmentTree fragments,
        PaginationOptions options,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken,
        Action<PaginationResult>? afterPaginate = null)
    {
        return DiagnosticStage.Run(
            diagnosticsSink,
            LayoutStageNames.Pagination,
            () =>
            {
                var pagination = _layoutPaginator.Paginate(fragments.Blocks, options, diagnosticsSink);
                afterPaginate?.Invoke(pagination);

                return pagination;
            },
            cancellationToken);
    }
}