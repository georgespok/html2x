using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Html2x.RenderModel.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Pagination.Test;

public sealed class LayoutPaginatorTests
{
    [Fact]
    public void Paginate_SecondBlockDoesNotFitCurrentPage_SplitsAtBlockBoundary()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 100f, 50f),
            CreateBlock(2, 60f, 100f, 40f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.TotalPlacements.ShouldBe(2);

        result.AuditPages[0].Placements.Count.ShouldBe(1);
        result.AuditPages[0].Placements[0].FragmentId.ShouldBe(1);
        result.AuditPages[0].Placements[0].Height.ShouldBe(50f);

        result.AuditPages[1].Placements.Count.ShouldBe(1);
        result.AuditPages[1].Placements[0].FragmentId.ShouldBe(2);
        result.AuditPages[1].Placements[0].Height.ShouldBe(40f);
        result.AuditPages[1].Placements[0].PageY.ShouldBe(10f);
    }

    [Fact]
    public void Paginate_BlockExactlyFitsRemainingSpace_DoesNotCreateExtraPage()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 100f, 30f),
            CreateBlock(2, 40f, 100f, 50f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.AuditPages[0].Placements.Count.ShouldBe(2);
        result.AuditPages[0].Placements[0].FragmentId.ShouldBe(1);
        result.AuditPages[0].Placements[1].FragmentId.ShouldBe(2);
        result.AuditPages[0].Placements[1].PageNumber.ShouldBe(1);
    }

    [Fact]
    public void Paginate_BlockStaysOnFirstPage_ClonesPlacedFragment()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var block = CreateBlock(7, 10f, 100f, 30f);

        // Act
        var result = Paginate(paginator, [block], new(200f, 100f), new(10f, 10f, 10f, 10f));

        // Assert
        var placed = result.Layout.Pages.ShouldHaveSingleItem().Children.ShouldHaveSingleItem()
            .ShouldBeOfType<BlockFragment>();
        placed.ShouldNotBeSameAs(block);
        placed.FragmentId.ShouldBe(block.FragmentId);
        placed.PageNumber.ShouldBe(1);
        placed.Rect.ShouldBe(block.Rect);
    }

    [Fact]
    public void Paginate_BlocksSpanMultiplePages_PreservesSourceOrderAcrossPages()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(11, 10f, 100f, 40f),
            CreateBlock(12, 50f, 100f, 40f),
            CreateBlock(13, 90f, 100f, 20f)
        };

        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);

        var flattened = result.AuditPages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .ToList();

        flattened.Select(static p => p.FragmentId).ShouldBe([11, 12, 13]);
        flattened.Select(static p => p.OrderIndex).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void Paginate_OverlappingSourceBlocks_UsesMonotonicPageY()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(21, 10f, 100f, 40f),
            CreateBlock(22, 20f, 100f, 30f),
            CreateBlock(23, 25f, 100f, 10f)
        };

        var pageSize = new SizePt(200f, 120f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 100

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.AuditPages[0].Placements.Count.ShouldBe(3);

        var placements = result.AuditPages[0].Placements;
        placements[1].PageY.ShouldBeGreaterThanOrEqualTo(placements[0].PageY + placements[0].Height);
        placements[2].PageY.ShouldBeGreaterThanOrEqualTo(placements[1].PageY + placements[1].Height);
    }

    [Fact]
    public void Paginate_BlockMovesToNextPage_EmitsOverflowDiagnosticsInOrder()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(31, 10f, 100f, 60f),
            CreateBlock(32, 70f, 100f, 30f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80
        var diagnostics = new RecordingDiagnosticsSink();

        // Act
        _ = Paginate(paginator, blocks, pageSize, margins, diagnostics);

        // Assert
        var traceEvents = diagnostics.Records.ToList();

        traceEvents.Select(static e => e.Name).ShouldBe([
            "layout/pagination/page-created",
            "layout/pagination/block-placed",
            "layout/pagination/block-moved-next-page",
            "layout/pagination/page-created",
            "layout/pagination/block-placed"
        ]);
        traceEvents.Select(static e => e.Severity).ShouldBe([
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Info
        ]);
        traceEvents.All(static e => e.Context is not null).ShouldBeTrue();

        var movedRecord = traceEvents[2];
        movedRecord.Fields["eventName"].ShouldBe(new DiagnosticStringValue("layout/pagination/block-moved-next-page"));
        movedRecord.Severity.ShouldBe(DiagnosticSeverity.Info);
        movedRecord.Context.ShouldNotBeNull();
        movedRecord.Context!.StructuralPath.ShouldBe("page[2]/fragment[32]");
        movedRecord.Fields["fromPage"].ShouldBe(new DiagnosticNumberValue(1));
        movedRecord.Fields["toPage"].ShouldBe(new DiagnosticNumberValue(2));
        movedRecord.Fields["fragmentId"].ShouldBe(new DiagnosticNumberValue(32));
        movedRecord.Fields["remainingSpace"].ShouldBe(new DiagnosticNumberValue(20f));

        var overflowPageRecord = traceEvents[3];
        overflowPageRecord.Fields["pageNumber"].ShouldBe(new DiagnosticNumberValue(2));
        overflowPageRecord.Fields["reason"].ShouldBe(new DiagnosticStringValue("Overflow"));
    }

    [Fact]
    public void Paginate_DiagnosticsSink_EmitsPaginationFieldRecords()
    {
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(31, 10f, 100f, 60f),
            CreateBlock(32, 70f, 100f, 30f)
        };
        var sink = new RecordingDiagnosticsSink();

        _ = Paginate(
            paginator,
            blocks,
            new(200f, 100f),
            new(10f, 10f, 10f, 10f),
            sink);

        var movedRecord =
            sink.Records.Single(static record => record.Name == "layout/pagination/block-moved-next-page");
        movedRecord.Stage.ShouldBe("stage/pagination");
        movedRecord.Severity.ShouldBe(DiagnosticSeverity.Info);
        movedRecord.Context.ShouldNotBeNull().StructuralPath.ShouldBe("page[2]/fragment[32]");
        movedRecord.Fields["fromPage"].ShouldBe(new DiagnosticNumberValue(1));
        movedRecord.Fields["toPage"].ShouldBe(new DiagnosticNumberValue(2));
        movedRecord.Fields["fragmentId"].ShouldBe(new DiagnosticNumberValue(32));
        movedRecord.Fields["remainingSpace"].ShouldBe(new DiagnosticNumberValue(20f));
        movedRecord.Fields["blockHeight"].ShouldBe(new DiagnosticNumberValue(30f));
    }

    [Fact]
    public void Paginate_InvokedTwiceWithSameInput_ProducesStableAssignments()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(41, 10f, 100f, 35f),
            CreateBlock(42, 50f, 100f, 35f),
            CreateBlock(43, 90f, 100f, 35f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var first = Paginate(paginator, blocks, pageSize, margins);
        var second = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        first.TotalPages.ShouldBe(second.TotalPages);
        first.TotalPlacements.ShouldBe(second.TotalPlacements);

        var firstPlacements = first.AuditPages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .Select(static p => (p.FragmentId, p.PageNumber, p.PageY, p.OrderIndex))
            .ToList();

        var secondPlacements = second.AuditPages
            .OrderBy(static p => p.PageNumber)
            .SelectMany(static p => p.Placements)
            .Select(static p => (p.FragmentId, p.PageNumber, p.PageY, p.OrderIndex))
            .ToList();

        firstPlacements.ShouldBe(secondPlacements);
    }

    [Fact]
    public void Paginate_InvokedTwiceWithSameInput_ProducesStableDiagnosticsContext()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(44, 10f, 100f, 35f),
            CreateBlock(45, 50f, 100f, 120f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f);
        var firstDiagnostics = new RecordingDiagnosticsSink();
        var secondDiagnostics = new RecordingDiagnosticsSink();

        // Act
        _ = Paginate(paginator, blocks, pageSize, margins, firstDiagnostics);
        _ = Paginate(paginator, blocks, pageSize, margins, secondDiagnostics);

        // Assert
        ExtractTraceContract(firstDiagnostics).ShouldBe(ExtractTraceContract(secondDiagnostics));
    }

    [Fact]
    public void Paginate_BlockIsOversized_PlacesWholeBlockOnNextPageAndMarksOversized()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(51, 10f, 100f, 60f),
            CreateBlock(52, 70f, 100f, 120f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.AuditPages[0].Placements.Select(static p => p.FragmentId).ShouldBe([51]);

        var oversized = result.AuditPages[1].Placements.ShouldHaveSingleItem();
        oversized.FragmentId.ShouldBe(52);
        oversized.PageY.ShouldBe(margins.Top);
        oversized.Height.ShouldBe(120f);
        oversized.IsOversized.ShouldBeTrue();
        oversized.DecisionKind.ShouldBe(PaginationDecisionKind.Oversized);
    }

    [Fact]
    public void Paginate_InputHasNoBlocks_ReturnsOneEmptyPage()
    {
        // Arrange
        // Empty (or whitespace-only) documents produce no block fragments from layout.
        var paginator = new LayoutPaginator();
        var blocks = Array.Empty<BlockFragment>();
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f);
        var diagnostics = new RecordingDiagnosticsSink();

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins, diagnostics);

        // Assert
        result.TotalPages.ShouldBe(1);
        result.TotalPlacements.ShouldBe(0);
        result.AuditPages[0].Placements.ShouldBeEmpty();

        diagnostics.Records
            .Select(static e => e.Name)
            .ShouldBe([
                "layout/pagination/page-created",
                "layout/pagination/empty-document"
            ]);
    }

    [Fact]
    public void Paginate_MovingBlocksToNextPage_ResetsCoordinatesToPageTop()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(61, 10f, 100f, 70f),
            CreateBlock(62, 650f, 100f, 20f),
            CreateBlock(63, 700f, 100f, 20f)
        };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.AuditPages[1].Placements.Count.ShouldBe(2);

        var secondPageFirst = result.AuditPages[1].Placements[0];
        var secondPageSecond = result.AuditPages[1].Placements[1];

        secondPageFirst.PageY.ShouldBe(margins.Top);
        secondPageFirst.PageY.ShouldNotBe(blocks[1].Rect.Y);
        secondPageSecond.PageY.ShouldBe(secondPageFirst.PageY + secondPageFirst.Height);
    }

    [Fact]
    public void Paginate_BlockMovesToNextPage_AlsoMovesChildLineCoordinates()
    {
        // Arrange
        var paginator = new LayoutPaginator();
        var first = CreateBlock(71, 10f, 100f, 70f);
        var moved = CreateBlockWithLine(
            72,
            650f,
            100f,
            40f,
            10f,
            "Moved");
        var blocks = new List<BlockFragment> { first, moved };
        var pageSize = new SizePt(200f, 100f);
        var margins = new Spacing(10f, 10f, 10f, 10f); // content height: 80

        // Act
        var result = Paginate(paginator, blocks, pageSize, margins);

        // Assert
        result.TotalPages.ShouldBe(2);
        var movedPlacement = result.AuditPages[1].Placements.ShouldHaveSingleItem();
        var movedBlock = result.Layout.Pages[1].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var line = movedBlock.Children.OfType<LineBoxFragment>().ShouldHaveSingleItem();

        movedPlacement.PageY.ShouldBe(margins.Top);
        movedPlacement.DecisionKind.ShouldBe(PaginationDecisionKind.MovedToNextPage);
        line.Rect.Y.ShouldBe(margins.Top + 10f);
        line.BaselineY.ShouldBe(margins.Top + 20f);
        line.Runs.ShouldHaveSingleItem().Origin.Y.ShouldBe(margins.Top + 20f);
    }

    private static PaginationResult Paginate(
        LayoutPaginator paginator,
        IReadOnlyList<BlockFragment> blocks,
        SizePt pageSize,
        Spacing margins,
        IDiagnosticsSink? diagnosticsSink = null) =>
        paginator.Paginate(
            blocks,
            new()
            {
                PageSize = pageSize,
                Margin = margins
            },
            diagnosticsSink);

    private static BlockFragment CreateBlock(int id, float y, float width, float height) =>
        new()
        {
            FragmentId = id,
            Rect = new(0f, y, width, height)
        };

    private static IReadOnlyList<(string Name, DiagnosticSeverity Severity, string? StructuralPath, string EventName)>
        ExtractTraceContract(RecordingDiagnosticsSink diagnostics)
    {
        return diagnostics.Records
            .Select(static e =>
            {
                var eventName = e.Fields["eventName"].ShouldBeOfType<DiagnosticStringValue>().Value;
                return (e.Name, e.Severity, e.Context?.StructuralPath, eventName);
            })
            .ToList();
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
            Rect = new(0f, lineY, width, 12f),
            BaselineY = baselineY,
            LineHeight = 12f,
            Runs =
            [
                new(
                    text,
                    font,
                    12f,
                    new(0f, baselineY),
                    30f,
                    9f,
                    3f)
            ]
        };

        return new([line])
        {
            FragmentId = id,
            Rect = new(0f, y, width, height)
        };
    }
}