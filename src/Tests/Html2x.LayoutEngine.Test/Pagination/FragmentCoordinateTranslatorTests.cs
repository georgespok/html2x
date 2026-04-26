using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Pagination;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Pagination;

public sealed class FragmentCoordinateTranslatorTests
{
    [Fact]
    public void BuiltInSupportedTypes_MatchKnownFragmentSet()
    {
        var actual = FragmentCoordinateTranslator.GetBuiltInSupportedTypes()
            .OrderBy(static type => type.Name)
            .ToList();

        var expected = new[]
        {
            typeof(BlockFragment),
            typeof(ImageFragment),
            typeof(LineBoxFragment),
            typeof(RuleFragment),
            typeof(TableCellFragment),
            typeof(TableFragment),
            typeof(TableRowFragment)
        }
        .OrderBy(static type => type.Name)
        .ToList();

        actual.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(BuiltInDescriptorCases))]
    public void BuiltInDescriptors_WhenFragmentTypeIsBuiltIn_ShouldDeclareExpectedDescriptor(
        Type fragmentType,
        string[] expectedGeometryFields,
        object expectedChildTraversal)
    {
        var descriptors = FragmentCoordinateTranslator.GetBuiltInDescriptors()
            .ToDictionary(static descriptor => descriptor.FragmentType);

        AssertDescriptor(
            descriptors,
            fragmentType,
            expectedGeometryFields,
            (FragmentChildTraversalPolicy)expectedChildTraversal);
    }

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

        var moved = CreateTranslator().CloneBlockWithPlacement(source, 3, 15f, 12f);
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

        var moved = CreateTranslator().CloneWithPlacement(source, 2, 15f, 12f)
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

        var moved = CreateTranslator().CloneWithPlacement(source, 2, 5f, 8f)
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
    public void CloneWithPlacement_RuleFragment_OffsetsRect()
    {
        var source = new RuleFragment
        {
            FragmentId = 1,
            PageNumber = 1,
            Rect = new RectangleF(10f, 20f, 40f, 2f)
        };

        var moved = CreateTranslator().CloneWithPlacement(source, 2, 8f, 7f)
            .ShouldBeOfType<RuleFragment>();

        moved.ShouldNotBeSameAs(source);
        moved.PageNumber.ShouldBe(2);
        moved.Rect.ShouldBe(new RectangleF(8f, 7f, 40f, 2f));
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

        var moved = CreateTranslator().CloneWithPlacement(source, 2, 5f, 8f)
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
    public void Paginate_BuiltInTableFragments_CloneTableSubtreeWithoutRegistration()
    {
        var paginator = new BlockPaginator();
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

        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            table
        };

        var result = paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f));
        var movedTable = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment.ShouldBeOfType<TableFragment>();

        movedTable.PageNumber.ShouldBe(2);
        movedTable.Rect.Y.ShouldBe(10f);
        movedTable.Rows.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells[0].Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>().Rect.Y.ShouldBe(25f);
    }

    [Fact]
    public void Paginate_MetadataRichFragments_PreserveAuthoritativeMetadata()
    {
        var paginator = new BlockPaginator();
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

        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            table
        };

        var result = paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f));
        var movedTable = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment.ShouldBeOfType<TableFragment>();
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
        var paginator = new BlockPaginator();
        var line = new LineBoxFragment
        {
            FragmentId = 201,
            PageNumber = 1,
            Rect = new RectangleF(5f, 655f, 60f, 14f),
            ZOrder = 3,
            Style = new VisualStyle(),
            BaselineY = 667f,
            LineHeight = 14f,
            TextAlign = "left",
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
            ZOrder = 2,
            Style = new VisualStyle()
        };
        var originalBlockRect = source.Rect;
        var originalLineRect = line.Rect;
        var originalBaseline = line.BaselineY;
        var originalRunOrigin = line.Runs.ShouldHaveSingleItem().Origin;

        var result = paginator.Paginate(
            [CreateBlock(1, 10f, 70f), source],
            new SizePt(200f, 100f),
            new Spacing(10f, 10f, 10f, 10f));

        var moved = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment;
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
    public void CloneWithPlacement_RichFragmentTree_PreservesProperties()
    {
        var style = new VisualStyle(
            Color: ColorRgba.Black,
            Borders: BorderEdges.Uniform(new BorderSide(1f, ColorRgba.Black, BorderLineStyle.Solid)),
            BackgroundColor: new ColorRgba(10, 20, 30, 255));
        var font = new FontKey("Test", FontWeight.W700, FontStyle.Italic);
        var line = new LineBoxFragment
        {
            FragmentId = 101,
            PageNumber = 1,
            Rect = new RectangleF(12f, 130f, 50f, 14f),
            ZOrder = 11,
            Style = style,
            BaselineY = 140f,
            LineHeight = 14f,
            TextAlign = "right",
            Runs =
            [
                new TextRun(
                    "alpha",
                    font,
                    12f,
                    new PointF(14f, 140f),
                    30f,
                    9f,
                    3f,
                    TextDecorations.Underline | TextDecorations.LineThrough,
                    ColorRgba.Black,
                    new ResolvedFont("Test", FontWeight.W700, FontStyle.Italic, "test", "test.ttf"))
            ]
        };
        var image = new ImageFragment
        {
            FragmentId = 102,
            PageNumber = 1,
            Rect = new RectangleF(70f, 132f, 25f, 20f),
            ZOrder = 12,
            Style = style,
            Src = "img.png",
            ContentRect = new RectangleF(72f, 134f, 21f, 16f),
            AuthoredSizePx = new SizePx(40, 30),
            IntrinsicSizePx = new SizePx(80, 60),
            IsMissing = true,
            IsOversize = true
        };
        var rule = new RuleFragment
        {
            FragmentId = 103,
            PageNumber = 1,
            Rect = new RectangleF(12f, 154f, 80f, 2f),
            ZOrder = 13,
            Style = style
        };
        var nestedBlock = new BlockFragment([rule])
        {
            FragmentId = 104,
            PageNumber = 1,
            Rect = new RectangleF(11f, 152f, 82f, 6f),
            ZOrder = 14,
            Style = style,
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 5f
        };
        var cell = new TableCellFragment([line, image, nestedBlock])
        {
            FragmentId = 100,
            PageNumber = 1,
            Rect = new RectangleF(10f, 128f, 90f, 35f),
            ZOrder = 10,
            Style = style,
            DisplayRole = FragmentDisplayRole.TableCell,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 4f,
            ColumnIndex = 3,
            IsHeader = true
        };
        var row = new TableRowFragment([cell])
        {
            FragmentId = 90,
            PageNumber = 1,
            Rect = new RectangleF(8f, 126f, 94f, 39f),
            ZOrder = 9,
            Style = style,
            DisplayRole = FragmentDisplayRole.TableRow,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 3f,
            RowIndex = 7
        };
        var table = new TableFragment([row])
        {
            FragmentId = 80,
            PageNumber = 1,
            Rect = new RectangleF(6f, 124f, 98f, 43f),
            ZOrder = 8,
            Style = style,
            DisplayRole = FragmentDisplayRole.Table,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 2f,
            DerivedColumnCount = 4
        };
        var block = new BlockFragment([table])
        {
            FragmentId = 70,
            PageNumber = 1,
            Rect = new RectangleF(0f, 120f, 110f, 60f),
            ZOrder = 7,
            Style = style,
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            MarkerOffset = 1f
        };

        var moved = CreateTranslator().CloneBlockWithPlacement(block, pageNumber: 2, x: 20f, y: 30f);
        var deltaX = moved.Rect.X - block.Rect.X;
        var deltaY = moved.Rect.Y - block.Rect.Y;

        AssertBlockPreserved(block, moved, 2, deltaX, deltaY);
        var movedTable = moved.Children.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        AssertTablePreserved(table, movedTable, 2, deltaX, deltaY);
        var movedRow = movedTable.Rows.ShouldHaveSingleItem();
        AssertTableRowPreserved(row, movedRow, 2, deltaX, deltaY);
        var movedCell = movedRow.Cells.ShouldHaveSingleItem();
        AssertTableCellPreserved(cell, movedCell, 2, deltaX, deltaY);
        AssertLinePreserved(line, movedCell.Children[0].ShouldBeOfType<LineBoxFragment>(), 2, deltaX, deltaY);
        AssertImagePreserved(image, movedCell.Children[1].ShouldBeOfType<ImageFragment>(), 2, deltaX, deltaY);
        var movedNestedBlock = movedCell.Children[2].ShouldBeOfType<BlockFragment>();
        AssertBlockPreserved(nestedBlock, movedNestedBlock, 2, deltaX, deltaY);
        AssertRulePreserved(rule, movedNestedBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<RuleFragment>(), 2, deltaX, deltaY);
    }

    [Fact]
    public void Paginate_UnknownFragmentTypeIsNotRegistered_ThrowWithExtensionGuidance()
    {
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            CreateBlockWithCustomChild<UnregisteredCustomFragment>(2, 650f, 40f)
        };

        var exception = Should.Throw<NotSupportedException>(() =>
            paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f)));

        exception.Message.ShouldContain(nameof(UnregisteredCustomFragment));
        exception.Message.ShouldContain(nameof(FragmentCoordinateTranslator.RegisterExtensionTranslator));
    }

    [Fact]
    public void Paginate_UnknownFragmentTypeIsRegistered_UseExtensionTranslator()
    {
        var translator = CreateTranslator();
        translator.RegisterExtensionDescriptor(
            FragmentTranslationDescriptor.Create<RegisteredCustomFragment>(
                [nameof(LayoutFragment.Rect)],
                FragmentChildTraversalPolicy.None,
                static (fragment, request, _) => new RegisteredCustomFragment
                {
                    FragmentId = fragment.FragmentId,
                    PageNumber = request.PageNumber,
                    Rect = new RectangleF(
                        fragment.Rect.X + request.DeltaX,
                        fragment.Rect.Y + request.DeltaY,
                        fragment.Rect.Width,
                        fragment.Rect.Height),
                    ZOrder = fragment.ZOrder,
                    Style = fragment.Style
                }));

        var paginator = new BlockPaginator(translator);
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            CreateBlockWithCustomChild<RegisteredCustomFragment>(2, 650f, 40f)
        };

        var result = paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f));
        var movedBlock = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment;
        var translatedCustom = movedBlock.Children.OfType<RegisteredCustomFragment>().ShouldHaveSingleItem();

        translatedCustom.PageNumber.ShouldBe(2);
        translatedCustom.Rect.Y.ShouldBe(15f); // child starts at block.Y + 5f after moving block to page top (10f)
    }

    private static FragmentCoordinateTranslator CreateTranslator()
    {
        return FragmentCoordinateTranslator.CreateDefault();
    }

    public static IEnumerable<object[]> BuiltInDescriptorCases()
    {
        yield return
        [
            typeof(BlockFragment),
            new[] { nameof(LayoutFragment.Rect), nameof(BlockFragment.Children) },
            FragmentChildTraversalPolicy.Children
        ];
        yield return
        [
            typeof(TableFragment),
            new[] { nameof(LayoutFragment.Rect), nameof(TableFragment.Rows) },
            FragmentChildTraversalPolicy.Rows
        ];
        yield return
        [
            typeof(TableRowFragment),
            new[] { nameof(LayoutFragment.Rect), nameof(TableRowFragment.Cells) },
            FragmentChildTraversalPolicy.Cells
        ];
        yield return
        [
            typeof(TableCellFragment),
            new[] { nameof(LayoutFragment.Rect), nameof(TableCellFragment.Children) },
            FragmentChildTraversalPolicy.Children
        ];
        yield return
        [
            typeof(LineBoxFragment),
            new[]
            {
                nameof(LayoutFragment.Rect),
                nameof(LineBoxFragment.OccupiedRect),
                nameof(LineBoxFragment.BaselineY),
                $"{nameof(LineBoxFragment.Runs)}.{nameof(TextRun.Origin)}"
            },
            FragmentChildTraversalPolicy.None
        ];
        yield return
        [
            typeof(ImageFragment),
            new[] { nameof(LayoutFragment.Rect), nameof(ImageFragment.ContentRect) },
            FragmentChildTraversalPolicy.None
        ];
        yield return
        [
            typeof(RuleFragment),
            new[] { nameof(LayoutFragment.Rect) },
            FragmentChildTraversalPolicy.None
        ];
    }

    private static BlockFragment CreateBlock(int id, float y, float height)
    {
        return new BlockFragment
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, 100f, height)
        };
    }

    private static BlockFragment CreateBlockWithCustomChild<TCustom>(int id, float y, float height)
        where TCustom : LayoutFragment, new()
    {
        var child = new TCustom
        {
            FragmentId = id * 100,
            Rect = new RectangleF(0f, y + 5f, 50f, 10f)
        };

        return new BlockFragment([child])
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, 100f, height)
        };
    }

    private sealed class UnregisteredCustomFragment : LayoutFragment
    {
    }

    private sealed class RegisteredCustomFragment : LayoutFragment
    {
    }

    private static void AssertDescriptor(
        IReadOnlyDictionary<Type, FragmentTranslationDescriptor> descriptors,
        Type fragmentType,
        string[] expectedGeometryFields,
        FragmentChildTraversalPolicy expectedChildTraversal)
    {
        descriptors.TryGetValue(fragmentType, out var descriptor).ShouldBeTrue();
        descriptor.ShouldNotBeNull();
        descriptor.GeometryFields.ShouldBe(expectedGeometryFields);
        descriptor.ChildTraversal.ShouldBe(expectedChildTraversal);
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

    private static void AssertTablePreserved(TableFragment source, TableFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertBlockPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.DerivedColumnCount.ShouldBe(source.DerivedColumnCount);
        moved.Rows.Count.ShouldBe(source.Rows.Count);
    }

    private static void AssertTableRowPreserved(TableRowFragment source, TableRowFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertBlockPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.RowIndex.ShouldBe(source.RowIndex);
        moved.Cells.Count.ShouldBe(source.Cells.Count);
    }

    private static void AssertTableCellPreserved(TableCellFragment source, TableCellFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertBlockPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.ColumnIndex.ShouldBe(source.ColumnIndex);
        moved.IsHeader.ShouldBe(source.IsHeader);
    }

    private static void AssertBlockPreserved(BlockFragment source, BlockFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertFragmentPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.DisplayRole.ShouldBe(source.DisplayRole);
        moved.FormattingContext.ShouldBe(source.FormattingContext);
        moved.MarkerOffset.ShouldBe(source.MarkerOffset);
        moved.Children.Count.ShouldBe(source.Children.Count);
    }

    private static void AssertLinePreserved(LineBoxFragment source, LineBoxFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertFragmentPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.BaselineY.ShouldBe(source.BaselineY + deltaY);
        moved.LineHeight.ShouldBe(source.LineHeight);
        moved.TextAlign.ShouldBe(source.TextAlign);
        moved.Runs.Count.ShouldBe(source.Runs.Count);

        var sourceRun = source.Runs.ShouldHaveSingleItem();
        var movedRun = moved.Runs.ShouldHaveSingleItem();
        movedRun.Text.ShouldBe(sourceRun.Text);
        movedRun.Font.ShouldBe(sourceRun.Font);
        movedRun.FontSizePt.ShouldBe(sourceRun.FontSizePt);
        movedRun.Origin.ShouldBe(new PointF(sourceRun.Origin.X + deltaX, sourceRun.Origin.Y + deltaY));
        movedRun.AdvanceWidth.ShouldBe(sourceRun.AdvanceWidth);
        movedRun.Ascent.ShouldBe(sourceRun.Ascent);
        movedRun.Descent.ShouldBe(sourceRun.Descent);
        movedRun.Decorations.ShouldBe(sourceRun.Decorations);
        movedRun.Color.ShouldBe(sourceRun.Color);
        movedRun.ResolvedFont.ShouldBe(sourceRun.ResolvedFont);
    }

    private static void AssertImagePreserved(ImageFragment source, ImageFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertFragmentPreserved(source, moved, pageNumber, deltaX, deltaY);
        moved.Src.ShouldBe(source.Src);
        moved.ContentRect.ShouldBe(Offset(source.ContentRect, deltaX, deltaY));
        moved.ContentSize.ShouldBe(source.ContentSize);
        moved.AuthoredSizePx.ShouldBe(source.AuthoredSizePx);
        moved.IntrinsicSizePx.ShouldBe(source.IntrinsicSizePx);
        moved.IsMissing.ShouldBe(source.IsMissing);
        moved.IsOversize.ShouldBe(source.IsOversize);
    }

    private static void AssertRulePreserved(RuleFragment source, RuleFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        AssertFragmentPreserved(source, moved, pageNumber, deltaX, deltaY);
    }

    private static void AssertFragmentPreserved(LayoutFragment source, LayoutFragment moved, int pageNumber, float deltaX, float deltaY)
    {
        moved.FragmentId.ShouldBe(source.FragmentId);
        moved.PageNumber.ShouldBe(pageNumber);
        moved.Rect.ShouldBe(Offset(source.Rect, deltaX, deltaY));
        moved.Size.ShouldBe(source.Size);
        moved.ZOrder.ShouldBe(source.ZOrder);
        moved.Style.ShouldBe(source.Style);
    }

    private static RectangleF Offset(RectangleF rect, float deltaX, float deltaY)
    {
        return new RectangleF(rect.X + deltaX, rect.Y + deltaY, rect.Width, rect.Height);
    }
}
