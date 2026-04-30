using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Creates a paged view of already measured block fragments.
/// Places whole block fragments across pages and translates fragment coordinates by cloning.
/// </summary>
public sealed class BlockPaginator
{
    private const string InitialPageReason = "InitialPage";
    private const string OverflowPageReason = "Overflow";
    private const float FitEpsilon = 0.001f;
    private readonly FragmentPlacementCloner _fragmentCloner;

    public BlockPaginator()
        : this(new FragmentPlacementCloner())
    {
    }

    internal BlockPaginator(FragmentPlacementCloner fragmentCloner)
    {
        _fragmentCloner = fragmentCloner ?? throw new ArgumentNullException(nameof(fragmentCloner));
    }

    public PaginationResult Paginate(
        IReadOnlyList<BlockFragment> blocks,
        SizePt pageSize,
        Spacing margins,
        IDiagnosticsSink? diagnosticsSink = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var orderedBlocks = ToDeterministicOrder(blocks);

        var contentArea = PageContentArea.From(pageSize, margins);
        var contentTop = contentArea.Y;
        var contentBottom = contentArea.Bottom;
        var paginationState = new PaginationBuildState(
            pageSize,
            margins,
            contentTop,
            contentBottom,
            GetInitialCursorY(orderedBlocks, contentTop),
            orderedBlocks.Count);

        PaginationDiagnostics.EmitPageCreated(
            diagnosticsSink,
            paginationState.PageNumber,
            InitialPageReason);

        if (orderedBlocks.Count == 0)
        {
            PaginationDiagnostics.EmitEmptyDocument(diagnosticsSink, paginationState.PageNumber);
        }

        var pageContentHeight = contentBottom - contentTop;

        for (var i = 0; i < orderedBlocks.Count; i++)
        {
            var block = orderedBlocks[i];
            var blockHeight = block.Rect.Height;
            var remainingSpace = paginationState.RemainingSpace;

            if (paginationState.ShouldStartNewPage(blockHeight))
            {
                paginationState.AdvanceToNextPage(
                    diagnosticsSink,
                    block,
                    blockHeight,
                    remainingSpace);
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
            }

            var placementY = ResolvePlacementY(block, paginationState.PageNumber, paginationState.CursorY);
            var placedFragment = CreatePlacedFragment(block, paginationState.PageNumber, placementY);
            paginationState.AddPlacement(CreatePlacement(placedFragment, paginationState.PageNumber, isOversized, i));

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

    private static PageModel CreatePage(
        int pageNumber,
        SizePt pageSize,
        Spacing margins,
        float contentTop,
        float contentBottom,
        IReadOnlyList<BlockFragmentPlacement> placements)
    {
        return new PageModel
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Margin = margins,
            ContentTop = contentTop,
            ContentBottom = contentBottom,
            Placements = placements
        };
    }

    private static bool FitsInRemainingSpace(float blockHeight, float remainingSpace)
    {
        return blockHeight - remainingSpace <= FitEpsilon;
    }

    private static IReadOnlyList<BlockFragment> ToDeterministicOrder(IReadOnlyList<BlockFragment> blocks)
    {
        // Materialize once to guarantee stable iteration across repeated runs.
        return blocks.ToArray();
    }

    private static bool IsOversizedBlock(float blockHeight, float pageContentHeight)
    {
        return blockHeight > pageContentHeight;
    }

    private static float GetRemainingSpace(float contentBottom, float cursorY)
    {
        return contentBottom - cursorY;
    }

    private BlockFragment CreatePlacedFragment(
        BlockFragment source,
        int pageNumber,
        float placementY)
    {
        return _fragmentCloner.CloneBlockWithPlacement(source, pageNumber, source.Rect.X, placementY);
    }

    private static float ResolvePlacementY(BlockFragment block, int pageNumber, float cursorY)
    {
        return pageNumber > 1
            ? GetNextPagePlacementY(cursorY)
            : GetCurrentPagePlacementY(block, cursorY);
    }

    private static float GetCurrentPagePlacementY(BlockFragment block, float cursorY)
    {
        return Math.Max(cursorY, block.Rect.Y);
    }

    private static float GetNextPagePlacementY(float cursorY)
    {
        return cursorY;
    }

    private static BlockFragmentPlacement CreatePlacement(
        BlockFragment fragment,
        int pageNumber,
        bool isOversized,
        int orderIndex)
    {
        return new BlockFragmentPlacement
        {
            FragmentId = fragment.FragmentId,
            PageNumber = pageNumber,
            IsOversized = isOversized,
            OrderIndex = orderIndex,
            Fragment = fragment
        };
    }

    private static float GetInitialCursorY(IReadOnlyList<BlockFragment> blocks, float contentTop)
    {
        return blocks.Count > 0 ? blocks[0].Rect.Y : contentTop;
    }

    private static PaginationResult CreateResult(IReadOnlyList<PageModel> pages)
    {
        return new PaginationResult
        {
            Pages = pages
        };
    }

    private sealed class PaginationBuildState(
        SizePt pageSize,
        Spacing margins,
        float contentTop,
        float contentBottom,
        float initialCursorY,
        int placementCapacity)
    {
        private readonly List<PageModel> _pages = [];
        private List<BlockFragmentPlacement> _placements = new(placementCapacity);

        public int PageNumber { get; private set; } = 1;

        public float CursorY { get; private set; } = initialCursorY;

        public float RemainingSpace => GetRemainingSpace(contentBottom, CursorY);

        public bool ShouldStartNewPage(float blockHeight)
        {
            return _placements.Count > 0 && !FitsInRemainingSpace(blockHeight, RemainingSpace);
        }

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
            CursorY = contentTop;
            PaginationDiagnostics.EmitPageCreated(diagnosticsSink, PageNumber, OverflowPageReason);
        }

        public IReadOnlyList<PageModel> Complete()
        {
            _pages.Add(CreateCurrentPage());
            return _pages;
        }

        private PageModel CreateCurrentPage()
        {
            return CreatePage(PageNumber, pageSize, margins, contentTop, contentBottom, _placements);
        }
    }
}
