using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Contracts.Style;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Stage.Contracts.Geometry;

namespace Html2x.LayoutEngine;

internal sealed class LayoutStageRunner
{
    private readonly FragmentTreeBuilder _fragmentTreeBuilder;
    private readonly ILayoutGeometryStage _layoutGeometryStage;
    private readonly LayoutPaginator _layoutPaginator;

    public LayoutStageRunner(
        ILayoutGeometryStage layoutGeometryStage,
        FragmentTreeBuilder fragmentTreeBuilder,
        LayoutPaginator layoutPaginator)
    {
        ArgumentNullException.ThrowIfNull(layoutGeometryStage);
        ArgumentNullException.ThrowIfNull(fragmentTreeBuilder);
        ArgumentNullException.ThrowIfNull(layoutPaginator);

        _layoutGeometryStage = layoutGeometryStage;
        _fragmentTreeBuilder = fragmentTreeBuilder;
        _layoutPaginator = layoutPaginator;
    }

    public PublishedLayoutTree BuildGeometry(
        StyleTree styleTree,
        LayoutGeometryRequest request,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        return DiagnosticStageRunner.Run(
            diagnosticsSink,
            LayoutStageNames.BoxTree,
            () => _layoutGeometryStage.Build(new(styleTree, request, diagnosticsSink)),
            cancellationToken);
    }

    public FragmentTree BuildFragmentTree(
        PublishedLayoutTree publishedLayout,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        return DiagnosticStageRunner.Run(
            diagnosticsSink,
            LayoutStageNames.FragmentTree,
            () => _fragmentTreeBuilder.Build(publishedLayout),
            cancellationToken);
    }

    public PaginationResult Paginate(
        FragmentTree fragments,
        PaginationOptions options,
        IDiagnosticsSink? diagnosticsSink,
        CancellationToken cancellationToken)
    {
        return DiagnosticStageRunner.Run(
            diagnosticsSink,
            LayoutStageNames.Pagination,
            () => _layoutPaginator.Paginate(fragments.Blocks, options, diagnosticsSink),
            cancellationToken);
    }
}
