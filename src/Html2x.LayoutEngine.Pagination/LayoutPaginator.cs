using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
///     Paginates measured layout fragments into page placement results.
/// </summary>
internal sealed class LayoutPaginator
{
    private readonly BlockPaginator _blockPaginator;

    /// <summary>
    ///     Initializes a new layout paginator using the current block-boundary placement algorithm.
    /// </summary>
    internal LayoutPaginator()
        : this(new())
    {
    }

    private LayoutPaginator(BlockPaginator blockPaginator)
    {
        _blockPaginator = blockPaginator;
    }

    /// <summary>
    ///     Paginates measured block fragments into final page layout and audit facts.
    /// </summary>
    /// <param name="blocks">Measured source block fragments in document order.</param>
    /// <param name="options">Page size and margin input facts.</param>
    /// <param name="diagnosticsSink">Optional diagnostics sink for pagination trace events.</param>
    /// <returns>The final layout plus stable pagination audit facts.</returns>
    internal PaginationResult Paginate(
        IReadOnlyList<BlockFragment> blocks,
        PaginationOptions options,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var plan = _blockPaginator.Paginate(blocks, options.PageSize, options.Margin, diagnosticsSink);
        return new()
        {
            Layout = CreateLayout(plan),
            AuditPages = CreateAuditPages(plan)
        };
    }

    private static HtmlLayout CreateLayout(BlockPaginationPlan plan)
    {
        var layout = new HtmlLayout();

        foreach (var page in plan.Pages)
        {
            layout.AddPage(new(
                page.PageSize,
                page.Margin,
                page.Placements.Select(static placement => (LayoutFragment)placement.Fragment).ToList(),
                page.PageNumber));
        }

        return layout;
    }

    private static IReadOnlyList<PaginationPageAudit> CreateAuditPages(BlockPaginationPlan plan)
    {
        return plan.Pages.Select(static page => new PaginationPageAudit
        {
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            Margin = page.Margin,
            ContentArea = page.ContentArea,
            Placements = page.Placements.Select(CreateAuditPlacement).ToList()
        }).ToList();
    }

    private static PaginationPlacementAudit CreateAuditPlacement(BlockFragmentPlacement placement)
    {
        var fragment = placement.Fragment;

        return new()
        {
            FragmentId = placement.FragmentId,
            PageNumber = placement.PageNumber,
            PlacedRect = placement.PlacedRect,
            OrderIndex = placement.OrderIndex,
            DecisionKind = placement.DecisionKind,
            IsOversized = placement.IsOversized,
            FragmentKind = fragment.DisplayRole?.ToString() ?? fragment.GetType().Name,
            DisplayRole = fragment.DisplayRole,
            FormattingContext = fragment.FormattingContext,
            MarkerOffset = fragment.MarkerOffset,
            DerivedColumnCount = fragment is TableFragment table ? table.DerivedColumnCount : null,
            RowIndex = fragment is TableRowFragment row ? row.RowIndex : null,
            ColumnIndex = fragment is TableCellFragment cell ? cell.ColumnIndex : null,
            IsHeader = fragment is TableCellFragment headerCell ? headerCell.IsHeader : null
        };
    }
}