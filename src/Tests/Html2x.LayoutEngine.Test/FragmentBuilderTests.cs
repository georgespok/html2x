using AngleSharp;
using System.Drawing;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestDoubles;
using Html2x.LayoutEngine.Test.Assertions;
using Html2x.LayoutEngine.Test.Builders;
using Moq;
using Shouldly;
using CoreFragment = Html2x.Abstractions.Layout.Fragments.Fragment;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Test;

public class FragmentBuilderTests
{
    [Fact]
    public void Build_SingleBlock_CreatesBlockFragment()
    {
        // Arrange
        var boxTree = BuildBoxTree()
            .Block(10, 20, 100, 50)
            .Up()
            .BuildTree();

        // Act
        var fragments = BuildFragments(boxTree);

        // Assert
        var fragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);
        fragment.Rect.X.ShouldBe(10f);
        fragment.Rect.Y.ShouldBe(20f);
        fragment.Rect.Width.ShouldBe(100f);
        fragment.Rect.Height.ShouldBe(50f);
    }

    [Fact]
    public void Build_UsedGeometry_UsesGeometryRectForBlockFragments()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(10f, 20f, 100f, 50f),
                new Spacing(3f, 4f, 5f, 6f),
                new Spacing(1f, 1f, 1f, 1f),
                markerOffset: 8f)
        };

        var tree = new BoxTree();
        tree.Blocks.Add(block);

        var fragments = BuildFragments(tree);

        var fragment = fragments.Blocks.ShouldHaveSingleItem();
        fragment.Rect.ShouldBe(new RectangleF(10f, 20f, 100f, 50f));
        fragment.MarkerOffset.ShouldBe(8f);
    }

    [Fact]
    public void Build_WithoutUsedGeometry_Throws()
    {
        var tree = new BoxTree();
        tree.Blocks.Add(new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        });

        var exception = Should.Throw<InvalidOperationException>(() =>
            CreateFragmentBuilder().Build(tree, CreateContext().FontSource));

        exception.Message.ShouldContain("requires UsedGeometry");
    }

    [Fact]
    public void Build_BlockBorder_CreatesBlockFragment()
    {
        // Arrange
        var boxTree = BuildBoxTree()
            .Block(10, 20, 100, 50, style: new ComputedStyle
            {
                Borders = BorderEdges.Uniform(
                    new(0.75f, new ColorRgba(0, 0, 0, 255), BorderLineStyle.Solid ))
            })
            .Up()
            .BuildTree();

        // Act
        var fragments = BuildFragments(boxTree);

        // Assert
        var fragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);
        fragment.Style.Borders.ShouldBeEquivalentTo(
            BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
        );

    }

    [Fact]
    public void Build_DivContainingInlineSpanAndBlockParagraph_ConvertsAllToFragments()
    {
        // Arrange: Div with inline span (text) and block paragraph (text)
        var boxTree = BuildBoxTree()
            .Block(0, 0, 595, 200)
                .Inline("Span inside Div")
                .Block(0, 50, 595, 40)
                    .Inline("Paragraph inside Div")
                    .Up()
                .Up()
            .Up()
            .BuildTree();

        // Act
        var fragments = BuildFragments(boxTree);

        // Assert: One top-level BlockFragment for div
        var divFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        // Div fragment should preserve normalized child order: inline content before the paragraph block.
        AssertFragment(divFragment).HasChildCount(2);

        var spanLine = divFragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        spanLine.Runs.Count.ShouldBe(1);
        spanLine.Runs[0].Text.ShouldBe("Span inside Div");

        var pFragment = divFragment.Children[1].ShouldBeOfType<BlockFragment>();
        var pLine = AssertFragment(pFragment).HasChildCount(1).GetChild<LineBoxFragment>(0);
        pLine.Runs.Count.ShouldBe(1);
        pLine.Runs[0].Text.ShouldBe("Paragraph inside Div");
    }

    [Fact]
    public void Build_DeeplyNestedBlocks_ConvertsAllInlineTextToFragments()
    {
        // Arrange: Div → span + P → text + nested Div → nested span
        var boxTree = BuildBoxTree()
            .Block(0, 0, 595, 300)
                .Inline("Span inside Div")
                .Block(0, 50, 595, 200)
                    .Inline("Paragraph inside Div")
                    .Block(0, 100, 595, 80)
                        .Inline("Nested Span inside nested Div")
                        .Up()
                    .Up()
                .Up()
            .Up()
            .BuildTree();

        // Act
        var fragments = BuildFragments(boxTree);

        // Assert: One top-level BlockFragment
        var divFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        // Outer div preserves normalized child order: outer inline content before the nested block.
        AssertFragment(divFragment).HasChildCount(2);

        var outerSpanLine = divFragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        outerSpanLine.Runs[0].Text.ShouldBe("Span inside Div");

        var pFragment = divFragment.Children[1].ShouldBeOfType<BlockFragment>();
        AssertFragment(pFragment).HasChildCount(2);

        var pTextLine = pFragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        pTextLine.Runs[0].Text.ShouldBe("Paragraph inside Div");

        var nestedDivFragment = pFragment.Children[1].ShouldBeOfType<BlockFragment>();
        var nestedSpanLine = AssertFragment(nestedDivFragment).HasChildCount(1).GetChild<LineBoxFragment>(0);
        nestedSpanLine.Runs[0].Text.ShouldBe("Nested Span inside nested Div");
    }

    [Fact]
    public void Build_UnorderedList_AddsBulletMarkers()
    {
        var boxTree = new BoxTree();
        var ulBlock = new BlockBox(BoxRole.Block) { Element = CreateElement("ul") };

        ulBlock.Children.Add(new BlockBox(BoxRole.Block)
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox(BoxRole.Inline) { TextContent = "• " },
                new InlineBox(BoxRole.Inline) { TextContent = "item1" }
            }
        });

        ulBlock.Children.Add(new BlockBox(BoxRole.Block)
        {
            Element = CreateElement("li"),
            Children =
            {
                new InlineBox(BoxRole.Inline) { TextContent = "• " },
                new InlineBox(BoxRole.Inline) { TextContent = "item2" }
            }
        });

        boxTree.Blocks.Add(ulBlock);

        var fragments = BuildFragments(boxTree);

        var ulFragment = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        var liFragment1 = ulFragment.Children[0].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment1, "• ", "item1");

        var liFragment2 = ulFragment.Children[1].ShouldBeOfType<BlockFragment>();
        AssertLineContainsMarkerAndText(liFragment2, "• ", "item2");
    }

    [Fact]
    public void Build_InlineBlockBetweenInlineRuns_PreservesTextOrderAcrossFragments()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 300,
            Height = 120,
            Style = new ComputedStyle()
        };

        root.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "before",
            Parent = root,
            Style = new ComputedStyle()
        });

        var inlineBlock = new InlineBox(BoxRole.InlineBlock)
        {
            Parent = root,
            Style = new ComputedStyle()
        };

        var inlineBlockContent = new BlockBox(BoxRole.Block)
        {
            Parent = inlineBlock,
            IsAnonymous = true,
            IsInlineBlockContext = true,
            Style = new ComputedStyle(),
            Width = 120,
            Height = 20
        };

        inlineBlockContent.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "inner",
            Parent = inlineBlockContent,
            Style = new ComputedStyle()
        });

        inlineBlock.Children.Add(inlineBlockContent);
        root.Children.Add(inlineBlock);

        root.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "after",
            Parent = root,
            Style = new ComputedStyle()
        });

        var tree = new BoxTree();
        tree.Blocks.Add(root);

        var fragments = BuildFragments(tree);
        var parent = AssertFragmentTree(fragments).HasBlockCount(1).GetBlock(0);

        var orderedTexts = EnumerateTextRuns(parent).ToList();
        orderedTexts.ShouldBe(["before", "inner", "after"]);
    }

    [Fact]
    public void Build_TableStructure_EmitsTableFragmentsAndCellText()
    {
        var table = new TableBox(BoxRole.Table)
        {
            X = 10,
            Y = 20,
            Width = 200,
            Height = 60,
            Style = new ComputedStyle()
        };

        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            X = 10,
            Y = 20,
            Width = 200,
            Height = 30,
            Style = new ComputedStyle()
        };

        var headerCell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = CreateElement("th"),
            X = 10,
            Y = 20,
            Width = 100,
            Height = 30,
            Style = new ComputedStyle()
        };
        headerCell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = headerCell,
            TextContent = "A",
            Style = new ComputedStyle()
        });

        var dataCell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = CreateElement("td"),
            X = 110,
            Y = 20,
            Width = 100,
            Height = 30,
            Style = new ComputedStyle()
        };
        dataCell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = dataCell,
            TextContent = "B",
            Style = new ComputedStyle()
        });

        row.Children.Add(headerCell);
        row.Children.Add(dataCell);
        table.Children.Add(row);

        var tree = new BoxTree();
        tree.Blocks.Add(table);

        var fragments = BuildFragments(tree);
        var tableFragment = fragments.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();
        tableFragment.DerivedColumnCount.ShouldBe(2);

        var rowFragment = tableFragment.Rows.ShouldHaveSingleItem();
        rowFragment.RowIndex.ShouldBe(0);

        var renderedHeaderCell = rowFragment.Cells[0];
        renderedHeaderCell.ColumnIndex.ShouldBe(0);
        renderedHeaderCell.IsHeader.ShouldBeTrue();
        renderedHeaderCell.Children
            .OfType<LineBoxFragment>()
            .Any(line => line.Runs.Any(run => run.Text == "A"))
            .ShouldBeTrue();

        var renderedDataCell = rowFragment.Cells[1];
        renderedDataCell.ColumnIndex.ShouldBe(1);
        renderedDataCell.IsHeader.ShouldBeFalse();
        renderedDataCell.Children
            .OfType<LineBoxFragment>()
            .Any(line => line.Runs.Any(run => run.Text == "B"))
            .ShouldBeTrue();
    }

    [Fact]
    public void Build_HeaderAndBodyRows_PreserveHierarchyAndHeaderMetadata()
    {
        var table = new TableBox(BoxRole.Table)
        {
            X = 10,
            Y = 20,
            Width = 200,
            Height = 60,
            DerivedColumnCount = 2,
            Style = new ComputedStyle()
        };

        var headerRow = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            X = 10,
            Y = 20,
            Width = 200,
            Height = 30,
            RowIndex = 0,
            Style = new ComputedStyle()
        };
        headerRow.Children.Add(CreateTableCell(headerRow, "th", "Name", columnIndex: 0, isHeader: true, x: 10, y: 20));
        headerRow.Children.Add(CreateTableCell(headerRow, "th", "Status", columnIndex: 1, isHeader: true, x: 110, y: 20));

        var bodyRow = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            X = 10,
            Y = 50,
            Width = 200,
            Height = 30,
            RowIndex = 1,
            Style = new ComputedStyle()
        };
        bodyRow.Children.Add(CreateTableCell(bodyRow, "td", "Alpha", columnIndex: 0, isHeader: false, x: 10, y: 50));
        bodyRow.Children.Add(CreateTableCell(bodyRow, "td", "Ready", columnIndex: 1, isHeader: false, x: 110, y: 50));

        table.Children.Add(headerRow);
        table.Children.Add(bodyRow);

        var tree = new BoxTree();
        tree.Blocks.Add(table);

        var fragments = BuildFragments(tree);
        var tableFragment = fragments.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableFragment>();

        tableFragment.DerivedColumnCount.ShouldBe(2);
        tableFragment.Rows.Count.ShouldBe(2);
        tableFragment.Rows[0].RowIndex.ShouldBe(0);
        tableFragment.Rows[1].RowIndex.ShouldBe(1);
        tableFragment.Rows[0].Cells.Select(cell => cell.ColumnIndex).ShouldBe([0, 1]);
        tableFragment.Rows[1].Cells.Select(cell => cell.ColumnIndex).ShouldBe([0, 1]);
        tableFragment.Rows[0].Cells.All(static cell => cell.IsHeader).ShouldBeTrue();
        tableFragment.Rows[1].Cells.All(static cell => !cell.IsHeader).ShouldBeTrue();
        EnumerateTextRuns(tableFragment).ShouldBe(["Name", "Status", "Alpha", "Ready"]);
    }

    [Fact]
    public void Build_MetadataRichBlockTableRowAndCell_UsesFragmentStageOwner()
    {
        var listItem = new BlockBox(BoxRole.ListItem)
        {
            MarkerOffset = 12f,
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 0f, 140f, 24f),
                new Spacing(),
                new Spacing(),
                markerOffset: 12f)
        };

        var table = new TableBox(BoxRole.Table)
        {
            DerivedColumnCount = 2,
            MarkerOffset = 4f,
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 24f, 200f, 32f),
                new Spacing(),
                new Spacing(),
                markerOffset: 4f)
        };

        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = table,
            RowIndex = 3,
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 24f, 200f, 32f),
                new Spacing(),
                new Spacing())
        };

        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            ColumnIndex = 1,
            IsHeader = true,
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 24f, 100f, 32f),
                new Spacing(),
                new Spacing())
        };

        row.Children.Add(cell);
        table.Children.Add(row);

        var tree = new BoxTree();
        tree.Blocks.Add(listItem);
        tree.Blocks.Add(table);

        var fragments = BuildFragments(tree);
        var blockFragment = fragments.Blocks[0];
        var tableFragment = fragments.Blocks[1].ShouldBeOfType<TableFragment>();
        var rowFragment = tableFragment.Rows.ShouldHaveSingleItem();
        var cellFragment = rowFragment.Cells.ShouldHaveSingleItem();

        blockFragment.DisplayRole.ShouldBe(FragmentDisplayRole.ListItem);
        blockFragment.FormattingContext.ShouldBe(FormattingContextKind.Block);
        blockFragment.MarkerOffset.ShouldBe(12f);

        tableFragment.DisplayRole.ShouldBe(FragmentDisplayRole.Table);
        tableFragment.FormattingContext.ShouldBe(FormattingContextKind.Block);
        tableFragment.MarkerOffset.ShouldBe(4f);
        tableFragment.DerivedColumnCount.ShouldBe(2);

        rowFragment.DisplayRole.ShouldBe(FragmentDisplayRole.TableRow);
        rowFragment.FormattingContext.ShouldBe(FormattingContextKind.Block);
        rowFragment.RowIndex.ShouldBe(3);

        cellFragment.DisplayRole.ShouldBe(FragmentDisplayRole.TableCell);
        cellFragment.FormattingContext.ShouldBe(FormattingContextKind.Block);
        cellFragment.ColumnIndex.ShouldBe(1);
        cellFragment.IsHeader.ShouldBeTrue();

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(PaperSizes.A4, new Spacing(), fragments.Blocks));
        var snapshot = LayoutSnapshotMapper.From(layout);
        var snapshots = Flatten(snapshot.Pages[0].Fragments).ToList();

        snapshots
            .Where(static fragment => fragment.Kind is "block" or "table" or "table-row" or "table-cell")
            .ShouldAllBe(fragment =>
                fragment.MetadataOwner == "FragmentBuilder" &&
                fragment.MetadataConsumer == "LayoutSnapshotMapper");
    }

    [Fact]
    public void LayoutSnapshotPayload_RepeatedRuns_PreservesOrderAndIds()
    {
        var runs = new List<IReadOnlyList<string>>();
        var sequenceRuns = new List<IReadOnlyList<int>>();

        for (var iteration = 0; iteration < 3; iteration++)
        {
            var fragmentTree = BuildFragments(BuildAmbiguousTopLevelOrderTree());
            var layout = new HtmlLayout();
            layout.Pages.Add(new LayoutPage(PaperSizes.A4, new Spacing(), fragmentTree.Blocks));

            var snapshot = LayoutSnapshotMapper.From(layout);
            var topLevelTexts = snapshot.Pages[0].Fragments
                .Select(GetFirstText)
                .ToList();
            runs.Add(topLevelTexts);

            var sequenceIds = Flatten(snapshot.Pages[0].Fragments)
                .Select(static fragment => fragment.SequenceId)
                .ToList();
            sequenceRuns.Add(sequenceIds);
        }

        runs[1].ShouldBe(runs[0]);
        runs[2].ShouldBe(runs[0]);
        runs[0].ShouldBe(["beta", "alpha"]);

        sequenceRuns[1].ShouldBe(sequenceRuns[0]);
        sequenceRuns[2].ShouldBe(sequenceRuns[0]);
        sequenceRuns[0].ShouldBe(Enumerable.Range(1, sequenceRuns[0].Count).ToList());
    }

    [Fact]
    public void LayoutSnapshotMapper_TableFragments_PreservesTableSpecificMetadata()
    {
        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(
            PaperSizes.A4,
            new Spacing(),
            [
                new TableFragment([
                    new TableRowFragment([
                        new TableCellFragment([
                            new LineBoxFragment
                            {
                                Rect = new RectangleF(14f, 18f, 20f, 10f),
                                Runs = []
                            }
                        ])
                        {
                            Rect = new RectangleF(10f, 12f, 80f, 28f),
                            DisplayRole = FragmentDisplayRole.TableCell,
                            ColumnIndex = 0,
                            IsHeader = true
                        },
                        new TableCellFragment
                        {
                            Rect = new RectangleF(90f, 12f, 80f, 28f),
                            DisplayRole = FragmentDisplayRole.TableCell,
                            ColumnIndex = 1,
                            IsHeader = false
                        }
                    ])
                    {
                        Rect = new RectangleF(10f, 12f, 160f, 28f),
                        DisplayRole = FragmentDisplayRole.TableRow,
                        RowIndex = 0
                    },
                    new TableRowFragment
                    {
                        Rect = new RectangleF(10f, 40f, 160f, 28f),
                        DisplayRole = FragmentDisplayRole.TableRow,
                        RowIndex = 1
                    }
                ])
                {
                    Rect = new RectangleF(10f, 12f, 160f, 56f),
                    DisplayRole = FragmentDisplayRole.Table,
                    DerivedColumnCount = 2
                }
            ]));

        var snapshot = LayoutSnapshotMapper.From(layout);
        var table = snapshot.Pages[0].Fragments.ShouldHaveSingleItem();

        table.Kind.ShouldBe("table");
        table.DerivedColumnCount.ShouldBe(2);
        table.Children.Count.ShouldBe(2);
        table.Children[0].Kind.ShouldBe("table-row");
        table.Children[0].RowIndex.ShouldBe(0);
        table.Children[0].Children.Count.ShouldBe(2);
        table.Children[0].Children[0].Kind.ShouldBe("table-cell");
        table.Children[0].Children[0].ColumnIndex.ShouldBe(0);
        table.Children[0].Children[0].IsHeader.ShouldBe(true);
        table.Children[0].Children[1].ColumnIndex.ShouldBe(1);
        table.Children[0].Children[1].IsHeader.ShouldBe(false);
        table.Children[1].RowIndex.ShouldBe(1);
    }

    // Helpers
    private static FragmentBuilder CreateFragmentBuilder() => new FragmentBuilder();

    private static BlockBoxBuilder BuildBoxTree() => new BlockBoxBuilder();

    private static FragmentTree BuildFragments(BoxTree tree)
    {
        var context = CreateContext();
        EnsureUsedGeometry(tree);
        PrepareInlineLayouts(tree, context);
        return CreateFragmentBuilder().Build(tree, context.FontSource);
    }

    private static void EnsureUsedGeometry(BoxTree tree)
    {
        foreach (var block in tree.Blocks)
        {
            EnsureUsedGeometry(block);
        }
    }

    private static void EnsureUsedGeometry(BlockBox block)
    {
        if (block.UsedGeometry is null)
        {
            var padding = block.Style.Padding.Safe();
            var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();
            block.UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(block.X, block.Y, block.Width, block.Height),
                padding,
                border,
                markerOffset: block.MarkerOffset);
        }

        foreach (var child in block.Children.OfType<BlockBox>())
        {
            EnsureUsedGeometry(child);
        }
    }

    private static FragmentTestContext CreateContext()
    {
        var textMeasurer = new Mock<ITextMeasurer>();
        textMeasurer.Setup(x => x.MeasureWidth(It.IsAny<FontKey>(), It.IsAny<float>(), It.IsAny<string>()))
            .Returns(0f);
        textMeasurer.Setup(x => x.GetMetrics(It.IsAny<FontKey>(), It.IsAny<float>()))
            .Returns((0f, 0f));

        var fontSource = new Mock<IFontSource>();
        fontSource.Setup(x => x.Resolve(It.IsAny<FontKey>(), It.IsAny<string>()))
            .Returns(new ResolvedFont("Default", FontWeight.W400, FontStyle.Normal, "test"));

        return new FragmentTestContext(textMeasurer.Object, fontSource.Object);
    }

    private static void PrepareInlineLayouts(BoxTree boxTree, FragmentTestContext context)
    {
        var inlineEngine = new InlineLayoutEngine(new FontMetricsProvider(), context.TextMeasurer, new DefaultLineHeightStrategy());

        foreach (var block in boxTree.Blocks)
        {
            PrepareInlineLayout(block, inlineEngine);
        }
    }

    private static void PrepareInlineLayout(BlockBox block, InlineLayoutEngine inlineEngine)
    {
        var padding = block.Style.Padding.Safe();
        var border = Spacing.FromBorderEdges(block.Style.Borders).Safe();
        var contentLeft = block.X + padding.Left + border.Left;
        var contentTop = block.Y + padding.Top + border.Top;
        var contentWidth = block.Width > 0f
            ? Math.Max(0f, block.Width - padding.Horizontal - border.Horizontal)
            : float.PositiveInfinity;

        inlineEngine.Layout(
            block,
            new InlineLayoutRequest(
                contentLeft,
                contentTop,
                contentWidth));

        foreach (var child in block.Children.OfType<BlockBox>())
        {
            if (child is InlineBlockBoundaryBox)
            {
                continue;
            }

            if (child.IsAnonymous && child.Children.All(static grandChild => grandChild is InlineBox))
            {
                continue;
            }

            PrepareInlineLayout(child, inlineEngine);
        }
    }

    private static FragmentTreeAssertion AssertFragmentTree(FragmentTree tree)
    {
        return new FragmentTreeAssertion(tree);
    }

    private static FragmentAssertion AssertFragment(CoreFragment fragment)
    {
        return new FragmentAssertion(fragment);
    }

    private static AngleSharp.Dom.IElement CreateElement(string tag)
    {
        return BrowsingContext.New(Configuration.Default)
            .OpenNewAsync().Result.CreateElement(tag);
    }

    private static void AssertLineContainsMarkerAndText(BlockFragment fragment, string marker, string text)
    {
        fragment.Children.Count.ShouldBe(1, "Line box fragments should collapse marker and text into a single entry.");

        var line = fragment.Children[0].ShouldBeOfType<LineBoxFragment>();
        line.Runs.Count.ShouldBe(2);
        line.Runs[0].Text.ShouldBe(marker);
        line.Runs[1].Text.ShouldBe(text);

        foreach (var run in line.Runs)
        {
            run.Origin.Y.ShouldBe(line.BaselineY, 0.01);
        }
    }

    private static TableCellBox CreateTableCell(
        TableRowBox parent,
        string tagName,
        string text,
        int columnIndex,
        bool isHeader,
        float x,
        float y)
    {
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = parent,
            Element = CreateElement(tagName),
            X = x,
            Y = y,
            Width = 100,
            Height = 30,
            ColumnIndex = columnIndex,
            IsHeader = isHeader,
            Style = new ComputedStyle()
        };
        cell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = cell,
            TextContent = text,
            Style = new ComputedStyle()
        });

        return cell;
    }

    private static IEnumerable<string> EnumerateTextRuns(CoreFragment fragment)
    {
        if (fragment is LineBoxFragment line)
        {
            foreach (var run in line.Runs)
            {
                var text = run.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return text;
                }
            }
        }

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var text in EnumerateTextRuns(child))
            {
                yield return text;
            }
        }
    }

    private static BoxTree BuildAmbiguousTopLevelOrderTree()
    {
        var beta = new BlockBox(BoxRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 20,
            Style = new ComputedStyle()
        };
        beta.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "beta",
            Parent = beta,
            Style = new ComputedStyle()
        });

        var alpha = new BlockBox(BoxRole.Block)
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 20,
            Style = new ComputedStyle()
        };
        alpha.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "alpha",
            Parent = alpha,
            Style = new ComputedStyle()
        });

        var tree = new BoxTree();
        tree.Blocks.Add(beta);
        tree.Blocks.Add(alpha);
        return tree;
    }

    private static string GetFirstText(Abstractions.Diagnostics.FragmentSnapshot fragment)
    {
        if (!string.IsNullOrWhiteSpace(fragment.Text))
        {
            return fragment.Text.Trim();
        }

        foreach (var child in fragment.Children)
        {
            var text = GetFirstText(child);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<Abstractions.Diagnostics.FragmentSnapshot> Flatten(IReadOnlyList<Abstractions.Diagnostics.FragmentSnapshot> fragments)
    {
        foreach (var fragment in fragments)
        {
            yield return fragment;

            foreach (var child in Flatten(fragment.Children))
            {
                yield return child;
            }
        }
    }

    private sealed record FragmentTestContext(ITextMeasurer TextMeasurer, IFontSource FontSource);
}
