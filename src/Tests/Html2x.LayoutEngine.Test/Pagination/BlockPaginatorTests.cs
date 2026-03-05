using System.Drawing;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Pagination;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Pagination;

public sealed class BlockPaginatorTests
{
    [Fact]
    public void Paginate_WhenSecondBlockDoesNotFitCurrentPage_SplitsAtBlockBoundary()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, y: 10f, width: 100f, height: 50f),
            CreateBlock(2, y: 60f, width: 100f, height: 40f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.TotalPlacements.ShouldBe(2);

        result.Pages[0].Placements.Count.ShouldBe(1);
        result.Pages[0].Placements[0].FragmentId.ShouldBe(1);
        result.Pages[0].Placements[0].Height.ShouldBe(50f);

        result.Pages[1].Placements.Count.ShouldBe(1);
        result.Pages[1].Placements[0].FragmentId.ShouldBe(2);
        result.Pages[1].Placements[0].Height.ShouldBe(40f);
        result.Pages[1].Placements[0].LocalY.ShouldBe(10f);
    }

    [Fact]
    public void Paginate_WhenBlockExactlyFitsRemainingSpace_DoesNotCreateExtraPage()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, y: 10f, width: 100f, height: 30f),
            CreateBlock(2, y: 40f, width: 100f, height: 50f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.Pages[0].Placements.Count.ShouldBe(2);
        result.Pages[0].Placements[0].FragmentId.ShouldBe(1);
        result.Pages[0].Placements[1].FragmentId.ShouldBe(2);
        result.Pages[0].Placements[1].PageNumber.ShouldBe(1);
    }

    [Fact]
    public void Paginate_WhenBlocksSpanMultiplePages_PreservesSourceOrderAcrossPages()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(11, y: 10f, width: 100f, height: 40f),
            CreateBlock(12, y: 50f, width: 100f, height: 40f),
            CreateBlock(13, y: 90f, width: 100f, height: 20f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);

        var flattened = result.Pages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .ToList();

        flattened.Select(static p => p.FragmentId).ShouldBe([11, 12, 13]);
        flattened.Select(static p => p.OrderIndex).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void Paginate_WhenSourceBlockPositionsOverlap_UsesMonotonicNonOverlappingYPerPage()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(21, y: 10f, width: 100f, height: 40f),
            CreateBlock(22, y: 20f, width: 100f, height: 30f),
            CreateBlock(23, y: 25f, width: 100f, height: 10f)
        };

        var pageSize = new SizePt(200f, 120f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 100

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.Pages[0].Placements.Count.ShouldBe(3);

        var placements = result.Pages[0].Placements;
        placements[1].LocalY.ShouldBeGreaterThanOrEqualTo(placements[0].LocalY + placements[0].Height);
        placements[2].LocalY.ShouldBeGreaterThanOrEqualTo(placements[1].LocalY + placements[1].Height);
    }

    [Fact]
    public void Paginate_WhenBlockMovesToNextPage_EmitsOverflowDiagnosticsInOrder()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(31, y: 10f, width: 100f, height: 60f),
            CreateBlock(32, y: 70f, width: 100f, height: 30f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80
        var diagnostics = new DiagnosticsSession();

        // Act
        _ = paginator.Paginate(blocks, pageSize, margins, diagnostics);

        // Assert
        var traceEvents = diagnostics.Events
            .Where(static e => e.Type == DiagnosticsEventType.Trace)
            .ToList();

        traceEvents.Select(static e => e.Name).ShouldBe([
            "layout/pagination/page-created",
            "layout/pagination/block-placed",
            "layout/pagination/block-moved-next-page",
            "layout/pagination/page-created",
            "layout/pagination/block-placed"
        ]);

        var movedPayload = (PaginationTracePayload)traceEvents[2].Payload!;
        movedPayload.FromPage.ShouldBe(1);
        movedPayload.ToPage.ShouldBe(2);
        movedPayload.FragmentId.ShouldBe(32);
        movedPayload.RemainingSpace.ShouldBe(20f);

        var overflowPagePayload = (PaginationTracePayload)traceEvents[3].Payload!;
        overflowPagePayload.PageNumber.ShouldBe(2);
        overflowPagePayload.Reason.ShouldBe("Overflow");
    }

    [Fact]
    public void Paginate_WhenInvokedTwiceWithSameInput_ProducesStableAssignments()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(41, y: 10f, width: 100f, height: 35f),
            CreateBlock(42, y: 50f, width: 100f, height: 35f),
            CreateBlock(43, y: 90f, width: 100f, height: 35f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var first = paginator.Paginate(blocks, pageSize, margins);
        var second = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        first.TotalPages.ShouldBe(second.TotalPages);
        first.TotalPlacements.ShouldBe(second.TotalPlacements);

        var firstPlacements = first.Pages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .Select(static p => (p.FragmentId, p.PageNumber, p.LocalY, p.OrderIndex))
            .ToList();

        var secondPlacements = second.Pages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .Select(static p => (p.FragmentId, p.PageNumber, p.LocalY, p.OrderIndex))
            .ToList();

        firstPlacements.ShouldBe(secondPlacements);
    }

    [Fact]
    public void Paginate_WhenBlockIsOversized_PlacesWholeBlockOnNextPageAndMarksOversized()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(51, y: 10f, width: 100f, height: 60f),
            CreateBlock(52, y: 70f, width: 100f, height: 120f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.Pages[0].Placements.Select(static p => p.FragmentId).ShouldBe([51]);

        var oversized = result.Pages[1].Placements.ShouldHaveSingleItem();
        oversized.FragmentId.ShouldBe(52);
        oversized.LocalY.ShouldBe(margins.Top);
        oversized.Height.ShouldBe(120f);
        oversized.IsOversized.ShouldBeTrue();
    }

    [Fact]
    public void Paginate_WhenInputHasNoBlocks_ReturnsOneEmptyPage()
    {
        // Arrange
        // Empty (or whitespace-only) documents produce no block fragments from layout.
        var paginator = new BlockPaginator();
        var blocks = Array.Empty<BlockFragment>();
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f);
        var diagnostics = new DiagnosticsSession();

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins, diagnostics);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.TotalPlacements.ShouldBe(0);
        result.Pages[0].Placements.ShouldBeEmpty();

        diagnostics.Events
            .Where(static e => e.Type == DiagnosticsEventType.Trace)
            .Select(static e => e.Name)
            .ShouldBe([
                "layout/pagination/page-created",
                "layout/pagination/empty-document"
            ]);
    }

    [Fact]
    public void Paginate_WhenMovingBlocksToNextPage_ResetsCoordinatesToPageTop()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(61, y: 10f, width: 100f, height: 70f),
            CreateBlock(62, y: 650f, width: 100f, height: 20f),
            CreateBlock(63, y: 700f, width: 100f, height: 20f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.Pages[1].Placements.Count.ShouldBe(2);

        var secondPageFirst = result.Pages[1].Placements[0];
        var secondPageSecond = result.Pages[1].Placements[1];

        secondPageFirst.LocalY.ShouldBe(margins.Top);
        secondPageFirst.LocalY.ShouldNotBe(blocks[1].Rect.Y);
        secondPageSecond.LocalY.ShouldBe(secondPageFirst.LocalY + secondPageFirst.Height);
    }

    [Fact]
    public void Paginate_WhenBlockMovesToNextPage_AlsoMovesChildLineCoordinates()
    {
        // Arrange
        var paginator = new BlockPaginator();
        var first = CreateBlock(71, y: 10f, width: 100f, height: 70f);
        var moved = CreateBlockWithLine(
            id: 72,
            y: 650f,
            width: 100f,
            height: 40f,
            lineOffsetY: 10f,
            text: "Moved");
        var blocks = new List<BlockFragment> { first, moved };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = paginator.Paginate(blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        var movedPlacement = result.Pages[1].Placements.ShouldHaveSingleItem();
        var movedBlock = movedPlacement.Fragment;
        var line = movedBlock.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        movedPlacement.LocalY.ShouldBe(margins.Top);
        line.Rect.Y.ShouldBe(margins.Top + 10f);
        line.BaselineY.ShouldBe(margins.Top + 20f);
        line.Runs.ShouldHaveSingleItem().Origin.Y.ShouldBe(margins.Top + 20f);
    }

    private static BlockFragment CreateBlock(int id, float y, float width, float height)
    {
        return new BlockFragment
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, width, height)
        };
    }

    private static BlockFragment CreateBlockWithLine(
        int id,
        float y,
        float width,
        float height,
        float lineOffsetY,
        string text)
    {
        var lineY = y + lineOffsetY;
        var baselineY = lineY + 10f;
        var font = new FontKey("Test", FontWeight.W400, FontStyle.Normal);
        var line = new LineBoxFragment
        {
            FragmentId = id * 100,
            Rect = new RectangleF(0f, lineY, width, 12f),
            BaselineY = baselineY,
            LineHeight = 12f,
            Runs =
            [
                new TextRun(
                    text,
                    font,
                    12f,
                    new PointF(0f, baselineY),
                    30f,
                    9f,
                    3f)
            ]
        };

        return new BlockFragment
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, width, height),
            Children = [line]
        };
    }
}
