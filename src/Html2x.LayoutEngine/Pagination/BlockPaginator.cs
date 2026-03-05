using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Diagnostics;

namespace Html2x.LayoutEngine.Pagination;

/// <summary>
/// Creates a paged view of already measured block fragments.
/// Foundational implementation keeps all blocks on the first page.
/// </summary>
public sealed class BlockPaginator
{
    private const string InitialPageReason = "InitialPage";
    private const string OverflowPageReason = "Overflow";
    private const float FitEpsilon = 0.001f;
    private const float PositionEpsilon = 0.001f;

    public PaginationResult Paginate(
        IReadOnlyList<BlockFragment> blocks,
        SizePt pageSize,
        Spacing margins,
        DiagnosticsSession? diagnosticsSession = null)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var orderedBlocks = ToDeterministicOrder(blocks);

        var pageNumber = 1;
        var contentTop = margins.Top;
        var contentBottom = pageSize.Height - margins.Bottom;

        PaginationDiagnostics.EmitPageCreated(diagnosticsSession, pageNumber, InitialPageReason);

        if (orderedBlocks.Count == 0)
        {
            PaginationDiagnostics.EmitEmptyDocument(diagnosticsSession, pageNumber);
        }

        var pages = new List<PageModel>();
        var placements = new List<BlockFragmentPlacement>(orderedBlocks.Count);
        var cursorY = GetInitialCursorY(orderedBlocks, contentTop);
        var pageContentHeight = contentBottom - contentTop;

        for (var i = 0; i < orderedBlocks.Count; i++)
        {
            var block = orderedBlocks[i];
            var blockHeight = block.Rect.Height;
            var remainingSpace = GetRemainingSpace(contentBottom, cursorY);

            if (ShouldStartNewPage(placements, blockHeight, remainingSpace))
            {
                AdvanceToNextPage(
                    diagnosticsSession,
                    pages,
                    pageSize,
                    margins,
                    contentTop,
                    contentBottom,
                    block,
                    blockHeight,
                    remainingSpace,
                    ref pageNumber,
                    ref placements,
                    ref cursorY);
            }

            remainingSpace = GetRemainingSpace(contentBottom, cursorY);
            var isOversized = IsOversizedBlock(blockHeight, pageContentHeight);
            if (isOversized)
            {
                PaginationDiagnostics.EmitOversizedBlock(
                    diagnosticsSession,
                    pageNumber,
                    block.FragmentId,
                    blockHeight,
                    pageContentHeight);
            }

            var placementY = ResolvePlacementY(block, pageNumber, cursorY);
            var placedFragment = CreatePlacedFragment(block, pageNumber, placementY);
            placements.Add(CreatePlacement(placedFragment, pageNumber, isOversized, i));

            PaginationDiagnostics.EmitBlockPlaced(
                diagnosticsSession,
                pageNumber,
                placedFragment.FragmentId,
                placedFragment.Rect.Y,
                placedFragment.Rect.Height,
                remainingSpace,
                remainingSpace - placedFragment.Rect.Height);

            cursorY = placementY + placedFragment.Rect.Height;
        }

        pages.Add(CreatePage(pageNumber, pageSize, margins, contentTop, contentBottom, placements));

        return CreateResult(pages);
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
            Margins = margins,
            ContentTop = contentTop,
            ContentBottom = contentBottom,
            Placements = placements
        };
    }

    private static bool ShouldStartNewPage(
        IReadOnlyCollection<BlockFragmentPlacement> placements,
        float blockHeight,
        float remainingSpace)
    {
        return placements.Count > 0 && !FitsInRemainingSpace(blockHeight, remainingSpace);
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

    private static BlockFragment CreatePlacedFragment(
        BlockFragment source,
        int pageNumber,
        float placementY)
    {
        if (CanReuseSourcePlacement(source, pageNumber, placementY))
        {
            return source;
        }

        return FragmentCoordinateTranslator.CloneBlockWithPlacement(source, pageNumber, source.Rect.X, placementY);
    }

    private static bool CanReuseSourcePlacement(BlockFragment source, int pageNumber, float placementY)
    {
        return pageNumber == 1 && Math.Abs(source.Rect.Y - placementY) <= PositionEpsilon;
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

    private static void AdvanceToNextPage(
        DiagnosticsSession? diagnosticsSession,
        ICollection<PageModel> pages,
        SizePt pageSize,
        Spacing margins,
        float contentTop,
        float contentBottom,
        BlockFragment block,
        float blockHeight,
        float remainingSpace,
        ref int pageNumber,
        ref List<BlockFragmentPlacement> placements,
        ref float cursorY)
    {
        PaginationDiagnostics.EmitBlockMovedNextPage(
            diagnosticsSession,
            pageNumber,
            pageNumber + 1,
            block.FragmentId,
            remainingSpace,
            blockHeight);

        pages.Add(CreatePage(pageNumber, pageSize, margins, contentTop, contentBottom, placements));
        pageNumber++;
        placements = [];
        cursorY = contentTop;
        PaginationDiagnostics.EmitPageCreated(diagnosticsSession, pageNumber, OverflowPageReason);
    }

}
