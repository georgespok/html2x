using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Formatting;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Pagination;
using Html2x.LayoutEngine.Style;
using Html2x.Text;

namespace Html2x.LayoutEngine;

/// <summary>
/// Coordinates the deterministic HTML layout pipeline from DOM and style resolution
/// through box tree layout, fragment projection, pagination, and layout assembly.
/// </summary>
internal sealed class LayoutBuilder
{
    private readonly LayoutGeometryBuilder _layoutGeometryBuilder;
    private readonly FragmentBuilder _fragmentBuilder;
    private readonly IStyleTreeBuilder _styleTreeBuilder;
    private readonly IImageMetadataResolver _imageMetadataResolver;

    public LayoutBuilder(
        ITextMeasurer textMeasurer,
        IImageMetadataResolver imageMetadataResolver)
    {
        ArgumentNullException.ThrowIfNull(textMeasurer);
        ArgumentNullException.ThrowIfNull(imageMetadataResolver);

        var blockFormattingContext = new BlockFormattingContext();

        _styleTreeBuilder = new StyleTreeBuilder();
        _layoutGeometryBuilder = new LayoutGeometryBuilder(textMeasurer, blockFormattingContext);
        _fragmentBuilder = new FragmentBuilder();
        _imageMetadataResolver = imageMetadataResolver;
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

        var publishedLayout = DiagnosticStage.Run(
            diagnosticsSink,
            "stage/box-tree",
            () => _layoutGeometryBuilder.Build(
                styleTree,
                new LayoutGeometryRequest
                {
                    PageSize = settings.PageSize,
                    ImageMetadataResolver = _imageMetadataResolver,
                    HtmlDirectory = settings.HtmlDirectory,
                    MaxImageSizeBytes = settings.MaxImageSizeBytes
                },
                diagnosticsSink),
            cancellationToken);
        var fragments = DiagnosticStage.Run(
            diagnosticsSink,
            "stage/fragment-tree",
            () => _fragmentBuilder.Build(publishedLayout),
            cancellationToken);

        return DiagnosticStage.Run(
            diagnosticsSink,
            "stage/pagination",
            () => CreateHtmlLayout(settings, publishedLayout, fragments, diagnosticsSink),
            cancellationToken);
    }

    private static HtmlLayout CreateHtmlLayout(
        LayoutBuildSettings settings,
        PublishedLayoutTree publishedLayout,
        FragmentTree fragments,
        IDiagnosticsSink? diagnosticsSink)
    {
        var paginator = new LayoutPaginator();
        var pagination = paginator.Paginate(
            fragments.Blocks,
            new PaginationOptions
            {
                PageSize = settings.PageSize,
                Margin = publishedLayout.Page.Margin
            },
            diagnosticsSink);
        PublishGeometrySnapshot(publishedLayout, pagination, diagnosticsSink);
        return pagination.Layout;
    }

    private static void PublishGeometrySnapshot(
        PublishedLayoutTree publishedLayout,
        PaginationResult pagination,
        IDiagnosticsSink? diagnosticsSink)
    {
        diagnosticsSink?.Emit(new DiagnosticRecord(
            Stage: "stage/pagination",
            Name: "layout/geometry-snapshot",
            Severity: DiagnosticSeverity.Info,
            Message: null,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field(
                    "snapshot",
                    GeometrySnapshotMapper.ToDiagnosticObject(publishedLayout, pagination))),
            Timestamp: DateTimeOffset.UtcNow));
    }

}
