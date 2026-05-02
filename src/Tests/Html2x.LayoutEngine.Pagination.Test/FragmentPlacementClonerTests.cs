using System.Drawing;
using Html2x.RenderModel;
using Html2x.LayoutEngine.Pagination;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragment;

namespace Html2x.LayoutEngine.Pagination.Test;

public sealed class FragmentPlacementClonerTests
{
    [Fact]
    public void CloneWithPlacement_BlockFragment_OffsetsRectAndChildren()
    {
        var line = CreateLine(2, 12f, 24f);
        var source = new BlockFragment([line])
        {
            FragmentId = 1,
            PageNumber = 1,
            Rect = new RectangleF(10f, 20f, 100f, 30f),
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 4f
        };

        var moved = CreateCloner().CloneBlockWithPlacement(source, 3, 15f, 12f);
        var movedLine = moved.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        moved.ShouldNotBeSameAs(source);
        moved.PageNumber.ShouldBe(3);
        moved.Rect.ShouldBe(new RectangleF(15f, 12f, 100f, 30f));
        moved.DisplayRole.ShouldBe(source.DisplayRole);
        moved.FormattingContext.ShouldBe(source.FormattingContext);
        moved.MarkerOffset.ShouldBe(source.MarkerOffset);
        movedLine.ShouldNotBeSameAs(line);
        movedLine.Rect.ShouldBe(new RectangleF(17f, 16f, 50f, 12f));
    }

    [Fact]
    public void CloneWithPlacement_LineBoxFragment_OffsetsRectBaselineAndTextRuns()
    {
        var source = CreateLine(1, 10f, 20f);

        var moved = CreateCloner().CloneWithPlacement(source, 2, 15f, 12f)
            .ShouldBeOfType<LineBoxFragment>();

        moved.PageNumber.ShouldBe(2);
        moved.Rect.ShouldBe(new RectangleF(15f, 12f, 50f, 12f));
        moved.BaselineY.ShouldBe(22f);
        moved.LineHeight.ShouldBe(source.LineHeight);
        moved.Runs.ShouldHaveSingleItem().Origin.ShouldBe(new PointF(16f, 22f));
        source.Runs.ShouldHaveSingleItem().Origin.ShouldBe(new PointF(11f, 30f));
    }

    [Fact]
    public void CloneWithPlacement_ImageFragment_OffsetsRectAndContentRect()
    {
        var source = new ImageFragment
        {
            FragmentId = 1,
            PageNumber = 1,
            Rect = new RectangleF(10f, 20f, 40f, 30f),
            Src = "image.png",
            ContentRect = new RectangleF(12f, 23f, 36f, 24f),
            AuthoredSizePx = new SizePx(40d, 30d),
            IntrinsicSizePx = new SizePx(80d, 60d),
            IsMissing = true,
            IsOversize = true
        };

        var moved = CreateCloner().CloneWithPlacement(source, 2, 5f, 8f)
            .ShouldBeOfType<ImageFragment>();

        moved.PageNumber.ShouldBe(2);
        moved.Rect.ShouldBe(new RectangleF(5f, 8f, 40f, 30f));
        moved.ContentRect.ShouldBe(new RectangleF(7f, 11f, 36f, 24f));
        moved.Src.ShouldBe(source.Src);
        moved.AuthoredSizePx.ShouldBe(source.AuthoredSizePx);
        moved.IntrinsicSizePx.ShouldBe(source.IntrinsicSizePx);
        moved.IsMissing.ShouldBeTrue();
        moved.IsOversize.ShouldBeTrue();
    }

    [Fact]
    public void CloneWithPlacement_TableFragment_OffsetsRowsCellsAndNestedChildren()
    {
        var line = CreateLine(4, 15f, 28f);
        var cell = new TableCellFragment([line])
        {
            FragmentId = 3,
            Rect = new RectangleF(14f, 26f, 50f, 20f),
            ColumnIndex = 2,
            IsHeader = true
        };
        var row = new TableRowFragment([cell])
        {
            FragmentId = 2,
            Rect = new RectangleF(12f, 24f, 80f, 24f),
            RowIndex = 5
        };
        var source = new TableFragment([row])
        {
            FragmentId = 1,
            Rect = new RectangleF(10f, 20f, 100f, 30f),
            DerivedColumnCount = 3
        };

        var moved = CreateCloner().CloneWithPlacement(source, 2, 5f, 8f)
            .ShouldBeOfType<TableFragment>();
        var movedRow = moved.Rows.ShouldHaveSingleItem();
        var movedCell = movedRow.Cells.ShouldHaveSingleItem();
        var movedLine = movedCell.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        moved.Rect.ShouldBe(new RectangleF(5f, 8f, 100f, 30f));
        moved.DerivedColumnCount.ShouldBe(source.DerivedColumnCount);
        movedRow.Rect.ShouldBe(new RectangleF(7f, 12f, 80f, 24f));
        movedRow.RowIndex.ShouldBe(row.RowIndex);
        movedCell.Rect.ShouldBe(new RectangleF(9f, 14f, 50f, 20f));
        movedCell.ColumnIndex.ShouldBe(cell.ColumnIndex);
        movedCell.IsHeader.ShouldBeTrue();
        movedLine.Rect.ShouldBe(new RectangleF(10f, 16f, 50f, 12f));
    }

    [Fact]
    public void Paginate_TableFragmentMovesToNextPage_ClonesTableSubtree()
    {
        var table = new TableFragment([
            new TableRowFragment([
                new TableCellFragment([
                    new LineBoxFragment
                    {
                        FragmentId = 1001,
                        Rect = new RectangleF(0f, 665f, 20f, 10f),
                        Runs = []
                    }
                ])
                {
                    FragmentId = 1000,
                    Rect = new RectangleF(0f, 660f, 50f, 20f),
                    ColumnIndex = 0
                }
            ])
            {
                FragmentId = 100,
                Rect = new RectangleF(0f, 660f, 100f, 20f),
                RowIndex = 0
            }
        ])
        {
            FragmentId = 10,
            Rect = new RectangleF(0f, 650f, 100f, 40f),
            DerivedColumnCount = 1
        };

        var result = Paginate(
            [CreateBlock(1, 10f, 70f), table],
            new SizePt(200f, 100f),
            new Spacing(10f, 10f, 10f, 10f));
        var movedTable = result.Layout.Pages[1].Children.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();

        movedTable.PageNumber.ShouldBe(2);
        movedTable.Rect.Y.ShouldBe(10f);
        movedTable.Rows.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells[0].Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>().Rect.Y.ShouldBe(25f);
    }

    [Fact]
    public void Paginate_MetadataRichFragments_PreserveAuthoritativeMetadata()
    {
        var table = new TableFragment([
            new TableRowFragment([
                new TableCellFragment
                {
                    FragmentId = 1000,
                    Rect = new RectangleF(0f, 660f, 50f, 20f),
                    DisplayRole = FragmentDisplayRole.TableCell,
                    FormattingContext = FormattingContextKind.Block,
                    MarkerOffset = 3f,
                    ColumnIndex = 1,
                    IsHeader = true
                }
            ])
            {
                FragmentId = 100,
                Rect = new RectangleF(0f, 660f, 100f, 20f),
                DisplayRole = FragmentDisplayRole.TableRow,
                FormattingContext = FormattingContextKind.Block,
                MarkerOffset = 2f,
                RowIndex = 4
            }
        ])
        {
            FragmentId = 10,
            Rect = new RectangleF(0f, 650f, 100f, 40f),
            DisplayRole = FragmentDisplayRole.Table,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 1f,
            DerivedColumnCount = 2
        };

        var result = Paginate(
            [CreateBlock(1, 10f, 70f), table],
            new SizePt(200f, 100f),
            new Spacing(10f, 10f, 10f, 10f));
        var movedTable = result.Layout.Pages[1].Children.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        var movedRow = movedTable.Rows.ShouldHaveSingleItem();
        var movedCell = movedRow.Cells.ShouldHaveSingleItem();

        movedTable.DisplayRole.ShouldBe(table.DisplayRole);
        movedTable.FormattingContext.ShouldBe(table.FormattingContext);
        movedTable.MarkerOffset.ShouldBe(table.MarkerOffset);
        movedTable.DerivedColumnCount.ShouldBe(table.DerivedColumnCount);
        movedRow.DisplayRole.ShouldBe(table.Rows[0].DisplayRole);
        movedRow.FormattingContext.ShouldBe(table.Rows[0].FormattingContext);
        movedRow.MarkerOffset.ShouldBe(table.Rows[0].MarkerOffset);
        movedRow.RowIndex.ShouldBe(table.Rows[0].RowIndex);
        movedCell.DisplayRole.ShouldBe(table.Rows[0].Cells[0].DisplayRole);
        movedCell.FormattingContext.ShouldBe(table.Rows[0].Cells[0].FormattingContext);
        movedCell.MarkerOffset.ShouldBe(table.Rows[0].Cells[0].MarkerOffset);
        movedCell.ColumnIndex.ShouldBe(table.Rows[0].Cells[0].ColumnIndex);
        movedCell.IsHeader.ShouldBe(table.Rows[0].Cells[0].IsHeader);
    }

    [Fact]
    public void Paginate_BlockMovesToNextPage_DoesNotMutateSourceSubtree()
    {
        var line = new LineBoxFragment
        {
            FragmentId = 201,
            PageNumber = 1,
            Rect = new RectangleF(5f, 655f, 60f, 14f),
            BaselineY = 667f,
            LineHeight = 14f,
            Runs =
            [
                new TextRun(
                    "move",
                    new FontKey("Test", FontWeight.W400, FontStyle.Normal),
                    12f,
                    new PointF(7f, 665f),
                    35f,
                    9f,
                    3f)
            ]
        };
        var source = new BlockFragment([line])
        {
            FragmentId = 2,
            PageNumber = 1,
            Rect = new RectangleF(0f, 650f, 100f, 40f),
            Style = new VisualStyle()
        };
        var originalBlockRect = source.Rect;
        var originalLineRect = line.Rect;
        var originalBaseline = line.BaselineY;
        var originalRunOrigin = line.Runs.ShouldHaveSingleItem().Origin;

        var result = Paginate(
            [CreateBlock(1, 10f, 70f), source],
            new SizePt(200f, 100f),
            new Spacing(10f, 10f, 10f, 10f));

        var moved = result.Layout.Pages[1].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var movedLine = moved.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        moved.ShouldNotBeSameAs(source);
        movedLine.ShouldNotBeSameAs(line);
        moved.Rect.Y.ShouldBe(10f);
        movedLine.Rect.Y.ShouldBe(15f);
        source.Rect.ShouldBe(originalBlockRect);
        source.PageNumber.ShouldBe(1);
        line.Rect.ShouldBe(originalLineRect);
        line.BaselineY.ShouldBe(originalBaseline);
        line.Runs.ShouldHaveSingleItem().Origin.ShouldBe(originalRunOrigin);
    }

    [Fact]
    public void Paginate_UnknownFragmentType_ThrowsWithExplicitCloneGuidance()
    {
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            new([
                new CustomFragment
                {
                    FragmentId = 200,
                    Rect = new RectangleF(0f, 655f, 50f, 10f)
                }
            ])
            {
                FragmentId = 2,
                Rect = new RectangleF(0f, 650f, 100f, 40f)
            }
        };

        var exception = Should.Throw<NotSupportedException>(() =>
            Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f)));

        exception.Message.ShouldContain(nameof(CustomFragment));
        exception.Message.ShouldContain("explicit clone branch");
    }

    private static FragmentPlacementCloner CreateCloner()
    {
        return new FragmentPlacementCloner();
    }

    private static PaginationResult Paginate(
        IReadOnlyList<BlockFragment> blocks,
        SizePt pageSize,
        Spacing margins)
    {
        return new LayoutPaginator().Paginate(
            blocks,
            new PaginationOptions
            {
                PageSize = pageSize,
                Margin = margins
            });
    }

    private static BlockFragment CreateBlock(int id, float y, float height)
    {
        return new BlockFragment
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, 100f, height)
        };
    }

    private static LineBoxFragment CreateLine(int id, float x, float y)
    {
        return new LineBoxFragment
        {
            FragmentId = id,
            PageNumber = 1,
            Rect = new RectangleF(x, y, 50f, 12f),
            BaselineY = y + 10f,
            LineHeight = 12f,
            Runs =
            [
                new TextRun(
                    "text",
                    new FontKey("Test", FontWeight.W400, FontStyle.Normal),
                    12f,
                    new PointF(x + 1f, y + 10f),
                    20f,
                    8f,
                    3f)
            ]
        };
    }

    private sealed class CustomFragment : LayoutFragment
    {
    }
}
