using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Text;
using Moq;
using Shouldly;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using System.Drawing;
using Html2x.LayoutEngine.Geometry;
using Html2x.LayoutEngine.Test.TestDoubles;

namespace Html2x.LayoutEngine.Test;

public class BlockLayoutEngineTests
{
    private readonly ITextMeasurer _textMeasurer = new FakeTextMeasurer(1f, 9f, 3f);
    
    private static PageBox DefaultPage() => new()
    {
        Margin = new Spacing(0, 0, 0, 0),
        Size = new SizePt(200, 400)
    };

    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        var root = new BlockBoxBuilder()
            .Block(0, 0, 0, 0, style: new ComputedStyle())
                .WithPadding(top: 10f, bottom: 6f)
                .Inline("content")
            .BuildRoot();

        
        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        var block = result.Blocks.ShouldHaveSingleItem();
        block.InlineLayout.ShouldNotBeNull();
        block.Height.ShouldBe(block.InlineLayout!.TotalHeight + 10f + 6f, 0.01f);
    }

    [Fact]
    public void Layout_Block_PopulatesUsedGeometryAndKeepsCompatibilityAccessorsInSync()
    {
        var style = new ComputedStyle
        {
            Padding = new Spacing(4f, 5f, 6f, 7f),
            Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
        };

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        root.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = root,
            Style = style,
            MarkerOffset = 9f
        });

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var block = result.Blocks.ShouldHaveSingleItem();
        block.UsedGeometry.ShouldNotBeNull();

        var geometry = block.UsedGeometry!.Value;
        geometry.BorderBoxRect.ShouldBe(new RectangleF(block.X, block.Y, block.Width, block.Height));
        geometry.ContentBoxRect.X.ShouldBe(block.X + 9f);
        geometry.ContentBoxRect.Y.ShouldBe(block.Y + 6f);
        geometry.ContentBoxRect.Width.ShouldBe(block.Width - 16f);
        geometry.ContentBoxRect.Height.ShouldBe(block.Height - 14f);
        geometry.MarkerOffset.ShouldBe(9f);
    }

    [Fact]
    public void Layout_BlockContentGeometry_AccountsForPaddingBorderAndMarkerOffset()
    {
        var style = new ComputedStyle
        {
            Padding = new Spacing(3f, 4f, 5f, 7f),
            Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
        };

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var listItem = new BlockBox(BoxRole.ListItem)
        {
            Parent = root,
            Style = style,
            MarkerOffset = 11f
        };
        listItem.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = listItem,
            Style = new ComputedStyle(),
            TextContent = "content"
        });
        root.Children.Add(listItem);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var block = result.Blocks.ShouldHaveSingleItem();
        var geometry = block.UsedGeometry.ShouldNotBeNull();
        geometry.ContentBoxRect.ShouldBe(new RectangleF(
            block.X + 9f,
            block.Y + 5f,
            block.Width - 15f,
            block.Height - 12f));
        geometry.MarkerOffset.ShouldBe(11f);
        block.InlineLayout.ShouldNotBeNull();
        block.InlineLayout!.Segments[0].Lines[0].Rect.X.ShouldBe(geometry.ContentBoxRect.X + geometry.MarkerOffset);
    }

    [Fact]
    public void Layout_BlockHeightPolicy_AppliesExplicitHeightAfterFlowHeights()
    {
        var parent = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                HeightPt = 12f
            }
        };
        parent.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = parent,
            Style = new ComputedStyle
            {
                HeightPt = 30f
            }
        });
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        root.Children.Add(parent);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var laidOutParent = result.Blocks.ShouldHaveSingleItem();
        laidOutParent.Height.ShouldBe(12f);
        laidOutParent.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>().Height.ShouldBe(30f);
    }

    [Fact]
    public void BlockBox_PostLayoutSetters_DoNotMutateUsedGeometry()
    {
        var block = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            },
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 0f, 100f, 40f),
                new Spacing(),
                new Spacing(2f, 2f, 2f, 2f))
        };

        var originalGeometry = block.UsedGeometry;

        Should.Throw<InvalidOperationException>(() => block.X = 1f);
        Should.Throw<InvalidOperationException>(() => block.Y = 1f);
        Should.Throw<InvalidOperationException>(() => block.Width = 1f);
        Should.Throw<InvalidOperationException>(() => block.Height = 1f);
        Should.Throw<InvalidOperationException>(() => block.MarkerOffset = 1f);
        block.Padding = new Spacing(3f, 5f, 7f, 11f);

        block.UsedGeometry.ShouldBe(originalGeometry);
        block.UsedGeometry!.Value.ContentBoxRect.ShouldBe(new RectangleF(2f, 2f, 96f, 36f));
    }

    [Fact]
    public void Layout_ListItemMarkerOffset_ShiftsInlineContentOrigin()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var listItem = new BlockBox(BoxRole.ListItem)
        {
            Parent = root,
            Style = new ComputedStyle(),
            MarkerOffset = 12f
        };
        listItem.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = listItem,
            Style = new ComputedStyle(),
            TextContent = "content"
        });
        root.Children.Add(listItem);

        _ = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        listItem.InlineLayout.ShouldNotBeNull();
        var line = listItem.InlineLayout!.Segments[0].Lines[0];
        line.Rect.X.ShouldBe(12f);
        line.Rect.Width.ShouldBeLessThanOrEqualTo(188f);
    }


    [Fact]
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        var root = new BlockBoxBuilder()
            .Inline("root inline")
            .Block(style: new ComputedStyle())
            .Up()
            .BuildRoot();

        NormalizeForBlockLayout(root);

        
        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldSatisfyAllConditions(
            () => result.Blocks.Count.ShouldBe(2),
            () => result.Blocks[0].IsAnonymous.ShouldBeTrue(),
            () => result.Blocks[0].Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>().TextContent.ShouldBe("root inline"),
            () => result.Blocks[1].IsAnonymous.ShouldBeFalse());
    }

    [Fact]
    public void Layout_AnonymousBlock_DoesNotInheritWidthOrHeightConstraints()
    {
        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                WidthPt = 60f,
                MinWidthPt = 50f,
                MaxWidthPt = 70f,
                HeightPt = 40f,
                MinHeightPt = 30f,
                MaxHeightPt = 50f
            }
        };

        root.Children.Add(new InlineBox(BoxRole.Inline) { TextContent = "inline" });
        root.Children.Add(new BlockBox(BoxRole.Block) { Style = new ComputedStyle() });

        NormalizeForBlockLayout(root);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var anonymous = result.Blocks.FirstOrDefault(b => b.IsAnonymous);
        anonymous.ShouldNotBeNull();
        anonymous!.Style.WidthPt.ShouldBeNull();
        anonymous.Style.MinWidthPt.ShouldBeNull();
        anonymous.Style.MaxWidthPt.ShouldBeNull();
        anonymous.Style.HeightPt.ShouldBeNull();
        anonymous.Style.MinHeightPt.ShouldBeNull();
        anonymous.Style.MaxHeightPt.ShouldBeNull();
    }

    [Fact]
    public void Layout_BlockOnlyChildren_DoesNotCreateAnonymousBlocks()
    {
        var root = new BlockBoxBuilder()
            .Block(style: new())
            .Up()
            .Block(style: new())
            .BuildRoot();

        NormalizeForBlockLayout(root);

        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.Count.ShouldBe(2);
        result.Blocks[0].IsAnonymous.ShouldBeFalse();
        result.Blocks[1].IsAnonymous.ShouldBeFalse();
    }
    
    [Theory]
    [InlineData(150f, 150f)] // Smaller than page (200) -> Clamped
    [InlineData(300f, 200f)] // Larger than page (200) -> Page limited
    [InlineData(null, 200f)] // No max width -> Page limited
    public void Layout_RespectsMaxWidth_ClampingToAvailableWidth(float? maxWidthPt, float expectedWidth)
    {
        // Arrange
        var root = new BlockBoxBuilder()
            .Block(style: new ComputedStyle { MaxWidthPt = maxWidthPt })
            .BuildRoot();

        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldHaveSingleItem()
            .Width.ShouldBe(expectedWidth);
    }

    [Fact]
    public void Layout_TableNode_ProducesNestedTableRowAndCellBlocks()
    {
        var firstRowSource = new TableRowBox(BoxRole.TableRow) { Style = new ComputedStyle() };
        var firstCellSource = new TableCellBox(BoxRole.TableCell) { Parent = firstRowSource, Style = new ComputedStyle() };
        var secondCellSource = new TableCellBox(BoxRole.TableCell) { Parent = firstRowSource, Style = new ComputedStyle() };
        firstRowSource.Children.Add(firstCellSource);
        firstRowSource.Children.Add(secondCellSource);

        var secondRowSource = new TableRowBox(BoxRole.TableRow) { Style = new ComputedStyle() };
        var thirdCellSource = new TableCellBox(BoxRole.TableCell) { Parent = secondRowSource, Style = new ComputedStyle() };
        secondRowSource.Children.Add(thirdCellSource);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        root.Children.Add(firstRowSource);
        root.Children.Add(secondRowSource);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(BoxRole.Table);
        table.Width.ShouldBe(120f);
        table.Height.ShouldBe(40f);
        table.Children.Count.ShouldBe(2);

        var firstRow = table.Children[0].ShouldBeOfType<TableRowBox>();
        firstRow.ShouldBeSameAs(firstRowSource);
        firstRow.Role.ShouldBe(BoxRole.TableRow);
        firstRow.Y.ShouldBe(0f);
        firstRow.Children.Count.ShouldBe(2);

        var firstCell = firstRow.Children[0].ShouldBeOfType<TableCellBox>();
        firstCell.ShouldBeSameAs(firstCellSource);
        firstCell.Role.ShouldBe(BoxRole.TableCell);
        firstCell.X.ShouldBe(0f);
        firstCell.Width.ShouldBe(60f);

        var secondCell = firstRow.Children[1].ShouldBeOfType<TableCellBox>();
        secondCell.ShouldBeSameAs(secondCellSource);
        secondCell.X.ShouldBe(60f);
        secondCell.Width.ShouldBe(60f);

        var secondRow = table.Children[1].ShouldBeOfType<TableRowBox>();
        secondRow.ShouldBeSameAs(secondRowSource);
        secondRow.Role.ShouldBe(BoxRole.TableRow);
        secondRow.Y.ShouldBe(20f);
        var thirdCell = secondRow.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();
        thirdCell.ShouldBeSameAs(thirdCellSource);
        thirdCell.Role.ShouldBe(BoxRole.TableCell);
    }

    [Fact]
    public void Layout_TableMaterialization_PopulatesRowAndCellGeometry()
    {
        var rowSource = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle
            {
                Padding = new Spacing(1f, 2f, 3f, 4f)
            }
        };
        var cellSource = new TableCellBox(BoxRole.TableCell)
        {
            Parent = rowSource,
            Style = new ComputedStyle
            {
                Padding = new Spacing(2f, 4f, 6f, 8f)
            }
        };
        rowSource.Children.Add(cellSource);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        root.Children.Add(rowSource);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        table.UsedGeometry.ShouldNotBeNull();
        row.UsedGeometry.ShouldNotBeNull();
        cell.UsedGeometry.ShouldNotBeNull();

        row.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new RectangleF(row.X, row.Y, row.Width, row.Height));
        row.UsedGeometry.Value.ContentBoxRect.X.ShouldBe(row.X + 4f);
        row.UsedGeometry.Value.ContentBoxRect.Y.ShouldBe(row.Y + 1f);
        cell.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new RectangleF(cell.X, cell.Y, cell.Width, cell.Height));
        cell.UsedGeometry.Value.ContentBoxRect.X.ShouldBe(cell.X + 8f);
        cell.UsedGeometry.Value.ContentBoxRect.Y.ShouldBe(cell.Y + 2f);
    }

    [Fact]
    public void Layout_TablePaddingAndBorder_PlacesCellsInContentBox()
    {
        var rowSource = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle()
        };
        var cellSource = new TableCellBox(BoxRole.TableCell)
        {
            Parent = rowSource,
            Style = new ComputedStyle()
        };
        rowSource.Children.Add(cellSource);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 104f,
                Padding = new Spacing(4f, 5f, 6f, 7f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        root.Children.Add(rowSource);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        table.Width.ShouldBe(120f);
        table.Height.ShouldBe(34f);
        table.UsedGeometry!.Value.ContentBoxRect.ShouldBe(new RectangleF(9f, 6f, 104f, 20f));
        row.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new RectangleF(9f, 6f, 104f, 20f));
        cell.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new RectangleF(9f, 6f, 104f, 20f));
    }

    [Fact]
    public void Layout_TableCellContent_PlacesNestedBlockAtCellContentBox()
    {
        var sourceRow = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle()
        };
        var sourceCell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = sourceRow,
            Style = new ComputedStyle
            {
                Padding = new Spacing(3f, 4f, 5f, 6f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        var nestedBlock = new BlockBox(BoxRole.Block)
        {
            Parent = sourceCell,
            Style = new ComputedStyle
            {
                HeightPt = 7f
            }
        };
        sourceCell.Children.Add(nestedBlock);
        sourceRow.Children.Add(sourceCell);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        root.Children.Add(sourceRow);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var cell = result.Blocks
            .ShouldHaveSingleItem().ShouldBeOfType<TableBox>()
            .Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>()
            .Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();
        var cellContent = cell.UsedGeometry.ShouldNotBeNull().ContentBoxRect;
        var laidOutNestedBlock = cell.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();

        laidOutNestedBlock.ShouldBeSameAs(nestedBlock);
        laidOutNestedBlock.UsedGeometry.ShouldNotBeNull();
        laidOutNestedBlock.X.ShouldBe(cellContent.X);
        laidOutNestedBlock.Y.ShouldBe(cellContent.Y);
        laidOutNestedBlock.Width.ShouldBe(cellContent.Width);
    }

    [Fact]
    public void Layout_BlockContainerWithNestedTable_PreservesTableInChildFlow()
    {
        var tableRow = new TableRowBox(BoxRole.TableRow) { Style = new ComputedStyle() };
        var leftCell = new TableCellBox(BoxRole.TableCell) { Parent = tableRow, Style = new ComputedStyle() };
        var rightCell = new TableCellBox(BoxRole.TableCell) { Parent = tableRow, Style = new ComputedStyle() };
        tableRow.Children.Add(leftCell);
        tableRow.Children.Add(rightCell);

        leftCell.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = leftCell,
            Style = new ComputedStyle
            {
                HeightPt = 40f
            }
        });

        var section = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        var headingSource = new BlockBox(BoxRole.Block)
        {
            Parent = section,
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "H2"),
            Style = new ComputedStyle
            {
                HeightPt = 12f
            }
        };
        headingSource.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = headingSource,
            Style = new ComputedStyle(),
            TextContent = "Heading"
        });
        section.Children.Add(headingSource);
        section.Children.Add(new TableBox(BoxRole.Table)
        {
            Parent = section,
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "TABLE"),
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        });
        var table = (TableBox)section.Children[1];
        table.Children.Add(tableRow);

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle()
        };
        root.Children.Add(section);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var laidOutSection = result.Blocks.ShouldHaveSingleItem();
        laidOutSection.Height.ShouldBe(52f);
        laidOutSection.Children.Count.ShouldBe(2);

        var heading = laidOutSection.Children[0].ShouldBeOfType<BlockBox>();
        heading.Y.ShouldBe(0f);
        heading.Height.ShouldBe(12f);

        var laidOutTable = laidOutSection.Children[1].ShouldBeOfType<TableBox>();
        laidOutTable.Role.ShouldBe(BoxRole.Table);
        laidOutTable.Y.ShouldBe(12f);
        laidOutTable.Height.ShouldBe(40f);
        var laidOutRow = laidOutTable.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        laidOutRow.ShouldBeSameAs(tableRow);
        laidOutRow.Role.ShouldBe(BoxRole.TableRow);
    }

    [Fact]
    public void Layout_TableGeometry_MaterializesWithoutRecalculation()
    {
        var rowSource = new TableRowBox(BoxRole.TableRow) { Style = new ComputedStyle() };
        var cellSource = new TableCellBox(BoxRole.TableCell)
        {
            Parent = rowSource,
            Style = new ComputedStyle
            {
                Padding = new Spacing(7.5f, 7.5f, 7.5f, 7.5f)
            }
        };
        cellSource.Children.Add(new BlockBox(BoxRole.Block)
        {
            Parent = cellSource,
            Style = new ComputedStyle
            {
                HeightPt = 14.5f
            }
        });
        rowSource.Children.Add(cellSource);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        root.Children.Add(rowSource);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        row.Height.ShouldBe(29.5f);
        cell.Height.ShouldBe(29.5f);
        table.Height.ShouldBe(29.5f);
    }

    [Fact]
    public void Layout_TableCellContent_IsMappedIntoMaterializedCellTree()
    {
        var sourceRow = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle()
        };
        var sourceCell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = sourceRow,
            Style = new ComputedStyle()
        };
        sourceCell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = sourceCell,
            TextContent = "alpha",
            Style = new ComputedStyle()
        });

        var nestedBlock = new BlockBox(BoxRole.Block)
        {
            Parent = sourceCell,
            Style = new ComputedStyle()
        };
        nestedBlock.Children.Add(new InlineBox(BoxRole.Inline)
        {
            Parent = nestedBlock,
            TextContent = "beta",
            Style = new ComputedStyle()
        });
        sourceCell.Children.Add(nestedBlock);
        sourceRow.Children.Add(sourceCell);

        var root = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        root.Children.Add(sourceRow);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var cell = result.Blocks
            .ShouldHaveSingleItem().ShouldBeOfType<TableBox>()
            .Children.ShouldHaveSingleItem()
            .ShouldBeOfType<TableRowBox>()
            .Children.ShouldHaveSingleItem()
            .ShouldBeOfType<TableCellBox>();
        cell.ShouldBeSameAs(sourceCell);

        var inlineChild = cell.Children[0].ShouldBeOfType<InlineBox>();
        inlineChild.ShouldBeSameAs(sourceCell.Children[0]);
        inlineChild.TextContent.ShouldBe("alpha");

        var mappedNestedBlock = cell.Children[1].ShouldBeOfType<BlockBox>();
        mappedNestedBlock.ShouldBeSameAs(nestedBlock);
        mappedNestedBlock.Parent.ShouldBeSameAs(cell);
        mappedNestedBlock.Children.ShouldHaveSingleItem().ShouldBeOfType<InlineBox>().TextContent.ShouldBe("beta");
    }

    [Fact]
    public void Layout_UnsupportedTable_EmitsDiagnosticsAndSkipsRows()
    {
        var diagnosticsSession = new DiagnosticsSession();
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle()
        };
        var cellElement = new Mock<AngleSharp.Dom.IElement>();
        cellElement.SetupGet(x => x.TagName).Returns("TD");
        cellElement.Setup(x => x.HasAttribute(HtmlCssConstants.HtmlAttributes.Colspan)).Returns(true);
        cellElement.Setup(x => x.GetAttribute(HtmlCssConstants.HtmlAttributes.Colspan)).Returns("2");
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Parent = row,
            Element = cellElement.Object,
            Style = new ComputedStyle()
        };
        row.Children.Add(cell);

        var root = new TableBox(BoxRole.Table)
        {
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "TABLE"),
            Style = new ComputedStyle
            {
                WidthPt = 150f
            }
        };
        root.Children.Add(row);

        var result = CreateBlockLayoutEngine(diagnosticsSession).Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(BoxRole.Table);
        table.Height.ShouldBe(0f);
        table.Children.ShouldBeEmpty();
        diagnosticsSession.Events.Any(e =>
            e.Name == "layout/table/unsupported-structure" &&
            e.Payload is UnsupportedStructurePayload payload &&
            payload.StructureKind == HtmlCssConstants.HtmlAttributes.Colspan &&
            payload.Reason == "Table cell colspan is not supported.").ShouldBeTrue();
        diagnosticsSession.Events.Any(e =>
            e.Name == "layout/table" &&
            e.Payload is TableLayoutPayload payload &&
            payload.Outcome == "Unsupported" &&
            payload.RowCount == 1 &&
            payload.Reason == "Table cell colspan is not supported.").ShouldBeTrue();
    }
    
    private BlockLayoutEngine CreateBlockLayoutEngine(DiagnosticsSession? diagnosticsSession = null)
    {
        var imageResolver = new ImageLayoutResolver();
        var inlineEngine = new InlineLayoutEngine(
            new FontMetricsProvider(),
            _textMeasurer,
            new DefaultLineHeightStrategy(),
            new Html2x.LayoutEngine.Formatting.BlockFormattingContext(),
            imageResolver,
            diagnosticsSession);
        return new BlockLayoutEngine(inlineEngine, new TableLayoutEngine(inlineEngine, imageResolver), diagnosticsSession);
    }

    private static void NormalizeForBlockLayout(BoxNode root)
    {
        if (root is BlockBox block)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(block);
        }
    }
}
