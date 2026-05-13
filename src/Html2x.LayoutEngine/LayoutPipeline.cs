using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.RenderModel.Documents;
using Html2x.Text;

namespace Html2x.LayoutEngine;

/// <summary>
///     Coordinates the deterministic HTML layout pipeline from DOM and style resolution
///     through box tree layout, fragment tree building, pagination, and layout assembly.
/// </summary>
internal sealed class LayoutPipeline
{
    private readonly IImageMetadataResolver _imageMetadataResolver;
    private readonly LayoutStageRunner _stageRunner;
    private readonly StyleTreeBuilder _styleTreeBuilder;

    public LayoutPipeline(
        ITextMeasurer textMeasurer,
        IImageMetadataResolver imageMetadataResolver)
    {
        ArgumentNullException.ThrowIfNull(textMeasurer);
        ArgumentNullException.ThrowIfNull(imageMetadataResolver);

        _imageMetadataResolver = imageMetadataResolver;
        _styleTreeBuilder = new StyleTreeBuilder();
        _stageRunner = new(
            new(textMeasurer),
            new(),
            new());
    }

    public async Task<HtmlLayout> BuildAsync(
        string html,
        LayoutBuildSettings settings,
        IDiagnosticsSink? diagnosticsSink = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(settings);

        var styleTree = await _styleTreeBuilder.BuildAsync(
            html,
            settings.Style,
            cancellationToken,
            diagnosticsSink);

        cancellationToken.ThrowIfCancellationRequested();

        var publishedLayout = _stageRunner.BuildGeometry(
            styleTree,
            CreateGeometryRequest(settings),
            diagnosticsSink,
            cancellationToken);

        var fragments = _stageRunner.BuildFragmentTree(
            publishedLayout,
            diagnosticsSink,
            cancellationToken);

        var pagination = _stageRunner.Paginate(
            fragments,
            CreatePaginationOptions(settings, publishedLayout),
            diagnosticsSink,
            cancellationToken);

        GeometrySnapshotDiagnostics.Emit(publishedLayout, pagination, diagnosticsSink);

        return pagination.Layout;
    }

    private LayoutGeometryRequest CreateGeometryRequest(LayoutBuildSettings settings) =>
        new()
        {
            PageSize = settings.PageSize,
            ImageMetadataResolver = _imageMetadataResolver
        };

    private static PaginationOptions CreatePaginationOptions(
        LayoutBuildSettings settings,
        PublishedLayoutTree publishedLayout) =>
        new()
        {
            PageSize = settings.PageSize,
            Margin = publishedLayout.Page.Margin
        };
}
