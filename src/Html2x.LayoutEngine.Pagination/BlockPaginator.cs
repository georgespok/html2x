using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Geometry;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
///     Creates a paged view of already measured block fragments.
///     Places whole block fragments across pages and translates fragment coordinates by cloning.
/// </summary>
internal sealed class BlockPaginator
{
    private const float FitEpsilon = 0.001f;
    private readonly FragmentPlacementCloner _fragmentCloner;

    internal BlockPaginator()
        : this(new())
    {
    }

    internal BlockPaginator(FragmentPlacementCloner fragmentCloner)
    {
        _fragmentCloner = fragmentCloner ?? throw new ArgumentNullException(nameof(fragmentCloner));
    }

    internal BlockPaginationPlan Paginate(
        IReadOnlyList<BlockFragment> blocks,
        SizePt pageSize,
        Spacing margins,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var orderedBlocks = ToDeterministicOrder(blocks);

        var contentArea = PageContentArea.From(pageSize, margins);
        var auditContentArea = new RectPt(contentArea.X, contentArea.Y, contentArea.Width, contentArea.Height);
        var paginationState = new PaginationBuildState(
            pageSize,
            margins,
            auditContentArea,
            GetInitialCursorY(orderedBlocks, auditContentArea.Y),
            orderedBlocks.Count);

        PaginationDiagnostics.EmitPageCreated(
            diagnosticsSink,
            paginationState.PageNumber,
            PaginationDiagnosticNames.Reasons.InitialPage);

        if (orderedBlocks.Count == 0)
        {
            PaginationDiagnostics.EmitEmptyDocument(diagnosticsSink, paginationState.PageNumber);
        }

        var pageContentHeight = auditContentArea.Height;

        for (var i = 0; i < orderedBlocks.Count; i++)
        {
            var block = orderedBlocks[i];
            var blockHeight = block.Rect.Height;
            var remainingSpace = paginationState.RemainingSpace;
            var decisionKind = PaginationDecisionKind.Placed;

            if (paginationState.ShouldStartNewPage(blockHeight))
            {
                paginationState.AdvanceToNextPage(
                    diagnosticsSink,
                    block,
                    blockHeight,
                    remainingSpace);
                decisionKind = PaginationDecisionKind.MovedToNextPage;
            }

            remainingSpace = paginationState.RemainingSpace;
            var isOversized = IsOversizedBlock(blockHeight, pageContentHeight);
            if (isOversized)
            {
                PaginationDiagnostics.EmitOversizedBlock(
                    diagnosticsSink,
                    paginationState.PageNumber,
                    block.FragmentId,
                    blockHeight,
                    pageContentHeight);
                decisionKind = PaginationDecisionKind.Oversized;
            }

            var placementY = ResolvePlacementY(block, paginationState.PageNumber, paginationState.CursorY);
            var placedFragment = CreatePlacedFragment(block, paginationState.PageNumber, placementY);
            paginationState.AddPlacement(CreatePlacement(
                placedFragment,
                paginationState.PageNumber,
                decisionKind,
                isOversized,
                i));

            PaginationDiagnostics.EmitBlockPlaced(
                diagnosticsSink,
                paginationState.PageNumber,
                placedFragment.FragmentId,
                placedFragment.Rect.Y,
                placedFragment.Rect.Height,
                remainingSpace,
                remainingSpace - placedFragment.Rect.Height);

            paginationState.AdvanceCursorTo(placementY + placedFragment.Rect.Height);
        }

        return CreateResult(paginationState.Complete());
    }

    private static BlockPaginationPage CreatePage(
        int pageNumber,
        SizePt pageSize,
        Spacing margins,
        RectPt contentArea,
        IReadOnlyList<BlockFragmentPlacement> placements) =>
        new()
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Margin = margins,
            ContentArea = contentArea,
            Placements = placements
        };

    private static bool FitsInRemainingSpace(float blockHeight, float remainingSpace) =>
        blockHeight - remainingSpace <= FitEpsilon;

    private static IReadOnlyList<BlockFragment> ToDeterministicOrder(IReadOnlyList<BlockFragment> blocks) =>
        // Materialize once to guarantee stable iteration across repeated runs.
        blocks.ToArray();

    private static bool IsOversizedBlock(float blockHeight, float pageContentHeight) => blockHeight > pageContentHeight;

    private static float GetRemainingSpace(float contentBottom, float cursorY) => contentBottom - cursorY;

    private BlockFragment CreatePlacedFragment(
        BlockFragment source,
        int pageNumber,
        float placementY) =>
        _fragmentCloner.CloneBlockWithPlacement(source, pageNumber, source.Rect.X, placementY);

    private static float ResolvePlacementY(BlockFragment block, int pageNumber, float cursorY) =>
        pageNumber > 1
            ? GetNextPagePlacementY(cursorY)
            : GetCurrentPagePlacementY(block, cursorY);

    private static float GetCurrentPagePlacementY(BlockFragment block, float cursorY) =>
        Math.Max(cursorY, block.Rect.Y);

    private static float GetNextPagePlacementY(float cursorY) => cursorY;

    private static BlockFragmentPlacement CreatePlacement(
        BlockFragment fragment,
        int pageNumber,
        PaginationDecisionKind decisionKind,
        bool isOversized,
        int orderIndex) =>
        new()
        {
            FragmentId = fragment.FragmentId,
            PageNumber = pageNumber,
            DecisionKind = decisionKind,
            IsOversized = isOversized,
            OrderIndex = orderIndex,
            Fragment = fragment
        };

    private static float GetInitialCursorY(IReadOnlyList<BlockFragment> blocks, float contentTop) =>
        blocks.Count > 0 ? blocks[0].Rect.Y : contentTop;

    private static BlockPaginationPlan CreateResult(IReadOnlyList<BlockPaginationPage> pages) =>
        new()
        {
            Pages = pages
        };

    private sealed class PaginationBuildState(
        SizePt pageSize,
        Spacing margins,
        RectPt contentArea,
        float initialCursorY,
        int placementCapacity)
    {
        private readonly List<BlockPaginationPage> _pages = [];
        private List<BlockFragmentPlacement> _placements = new(placementCapacity);

        public int PageNumber { get; private set; } = 1;

        public float CursorY { get; private set; } = initialCursorY;

        public float RemainingSpace => GetRemainingSpace(contentArea.Bottom, CursorY);

        public bool ShouldStartNewPage(float blockHeight) =>
            _placements.Count > 0 && !FitsInRemainingSpace(blockHeight, RemainingSpace);

        public void AddPlacement(BlockFragmentPlacement placement)
        {
            _placements.Add(placement);
        }

        public void AdvanceCursorTo(float cursorY)
        {
            CursorY = cursorY;
        }

        public void AdvanceToNextPage(
            IDiagnosticsSink? diagnosticsSink,
            BlockFragment block,
            float blockHeight,
            float remainingSpace)
        {
            PaginationDiagnostics.EmitBlockMovedNextPage(
                diagnosticsSink,
                PageNumber,
                PageNumber + 1,
                block.FragmentId,
                remainingSpace,
                blockHeight);

            _pages.Add(CreateCurrentPage());
            PageNumber++;
            _placements = [];
            CursorY = contentArea.Y;
            PaginationDiagnostics.EmitPageCreated(diagnosticsSink, PageNumber,
                PaginationDiagnosticNames.Reasons.Overflow);
        }

        public IReadOnlyList<BlockPaginationPage> Complete()
        {
            _pages.Add(CreateCurrentPage());
            return _pages;
        }

        private BlockPaginationPage CreateCurrentPage() =>
            CreatePage(PageNumber, pageSize, margins, contentArea, _placements);
    }
}