using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.Abstractions.Diagnostics;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using System.Drawing;

namespace Html2x.LayoutEngine.Test;

public class BlockLayoutEngineTests
{
    private readonly Mock<IInlineLayoutEngine> _inlineEngine;
    private readonly Mock<ITableLayoutEngine> _tableLayoutEngine;
    private readonly Mock<IFloatLayoutEngine> _floatLayoutEngine;
    
    private static PageBox DefaultPage() => new()
    {
        Margin = new Spacing(0, 0, 0, 0),
        Size = new SizePt(200, 400)
    };

    public BlockLayoutEngineTests(ITestOutputHelper output) 
    {
        _inlineEngine = new Mock<IInlineLayoutEngine>();
        _tableLayoutEngine = new Mock<ITableLayoutEngine>();
        _floatLayoutEngine = new Mock<IFloatLayoutEngine>();
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(0f));
    }
    
    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        // Arrange
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(24f));

        var root = new BlockBoxBuilder()
            .Block(0, 0, 0, 0, style: new ComputedStyle())
                .WithPadding(top: 10f, bottom: 6f)
            .BuildRoot();

        
        // Act
        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        // Assert
        result.Blocks.ShouldHaveSingleItem()
            .Height.ShouldBe(24f + 10f + 6f);
    }

    [Fact]
    public void Layout_Block_PopulatesUsedGeometryAndKeepsCompatibilityAccessorsInSync()
    {
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(24f));

        var style = new ComputedStyle
        {
            Padding = new Spacing(4f, 5f, 6f, 7f),
            Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
        };

        var root = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle()
        };
        root.Children.Add(new BlockBox(DisplayRole.Block)
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
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        // Arrange
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(10f));

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
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(10f));

        var root = new BlockBox(DisplayRole.Block)
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

        root.Children.Add(new InlineBox(DisplayRole.Inline) { TextContent = "inline" });
        root.Children.Add(new BlockBox(DisplayRole.Block) { Style = new ComputedStyle() });

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
        // Arrange
        _inlineEngine.Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>())).Returns(CreateInlineLayoutResult(12f));

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
        var firstRowSource = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() };
        var firstCellSource = new TableCellBox(DisplayRole.TableCell) { Parent = firstRowSource, Style = new ComputedStyle() };
        var secondCellSource = new TableCellBox(DisplayRole.TableCell) { Parent = firstRowSource, Style = new ComputedStyle() };
        firstRowSource.Children.Add(firstCellSource);
        firstRowSource.Children.Add(secondCellSource);

        var secondRowSource = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() };
        var thirdCellSource = new TableCellBox(DisplayRole.TableCell) { Parent = secondRowSource, Style = new ComputedStyle() };
        secondRowSource.Children.Add(thirdCellSource);

        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 120f,
                DerivedColumnCount = 2,
                ColumnWidths = [60f, 60f],
                Height = 40f,
                Rows =
                [
                    new TableLayoutRowResult
                    {
                        SourceRow = firstRowSource,
                        RowIndex = 0,
                        Y = 0f,
                        Height = 20f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = firstCellSource,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 60f,
                                Height = 20f
                            },
                            new TableLayoutCellPlacement
                            {
                                SourceCell = secondCellSource,
                                ColumnIndex = 1,
                                X = 60f,
                                Y = 0f,
                                Width = 60f,
                                Height = 20f
                            }
                        ]
                    },
                    new TableLayoutRowResult
                    {
                        SourceRow = secondRowSource,
                        RowIndex = 1,
                        Y = 20f,
                        Height = 20f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = thirdCellSource,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 20f,
                                Width = 60f,
                                Height = 20f
                            }
                        ]
                    }
                ]
            });

        var root = new TableBox(DisplayRole.Table)
        {
            Style = new ComputedStyle()
        };
        root.Children.Add(firstRowSource);
        root.Children.Add(secondRowSource);

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(DisplayRole.Table);
        table.Width.ShouldBe(120f);
        table.Height.ShouldBe(40f);
        table.Children.Count.ShouldBe(2);

        var firstRow = table.Children[0].ShouldBeOfType<TableRowBox>();
        firstRow.ShouldBeSameAs(firstRowSource);
        firstRow.Role.ShouldBe(DisplayRole.TableRow);
        firstRow.Y.ShouldBe(0f);
        firstRow.Children.Count.ShouldBe(2);

        var firstCell = firstRow.Children[0].ShouldBeOfType<TableCellBox>();
        firstCell.ShouldBeSameAs(firstCellSource);
        firstCell.Role.ShouldBe(DisplayRole.TableCell);
        firstCell.X.ShouldBe(0f);
        firstCell.Width.ShouldBe(60f);

        var secondCell = firstRow.Children[1].ShouldBeOfType<TableCellBox>();
        secondCell.ShouldBeSameAs(secondCellSource);
        secondCell.X.ShouldBe(60f);
        secondCell.Width.ShouldBe(60f);

        var secondRow = table.Children[1].ShouldBeOfType<TableRowBox>();
        secondRow.ShouldBeSameAs(secondRowSource);
        secondRow.Role.ShouldBe(DisplayRole.TableRow);
        secondRow.Y.ShouldBe(20f);
        var thirdCell = secondRow.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();
        thirdCell.ShouldBeSameAs(thirdCellSource);
        thirdCell.Role.ShouldBe(DisplayRole.TableCell);
    }

    [Fact]
    public void Layout_TableMaterialization_PopulatesUsedGeometryForTableRowAndCellBlocks()
    {
        var rowSource = new TableRowBox(DisplayRole.TableRow)
        {
            Style = new ComputedStyle
            {
                Padding = new Spacing(1f, 2f, 3f, 4f)
            }
        };
        var cellSource = new TableCellBox(DisplayRole.TableCell)
        {
            Parent = rowSource,
            Style = new ComputedStyle
            {
                Padding = new Spacing(2f, 4f, 6f, 8f)
            }
        };
        rowSource.Children.Add(cellSource);

        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 120f,
                DerivedColumnCount = 1,
                ColumnWidths = [120f],
                Height = 20f,
                Rows =
                [
                    new TableLayoutRowResult
                    {
                        SourceRow = rowSource,
                        RowIndex = 0,
                        Y = 0f,
                        Height = 20f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = cellSource,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 120f,
                                Height = 20f
                            }
                        ]
                    }
                ]
            });

        var root = new TableBox(DisplayRole.Table)
        {
            Style = new ComputedStyle()
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
    public void Layout_BlockContainerWithNestedTable_PreservesTableInChildFlow()
    {
        var tableRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() };
        var leftCell = new TableCellBox(DisplayRole.TableCell) { Parent = tableRow, Style = new ComputedStyle() };
        var rightCell = new TableCellBox(DisplayRole.TableCell) { Parent = tableRow, Style = new ComputedStyle() };
        tableRow.Children.Add(leftCell);
        tableRow.Children.Add(rightCell);

        _inlineEngine
            .Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>()))
            .Returns<BlockBox, InlineLayoutRequest>((node, _) => CreateInlineLayoutResult(node.Element?.TagName == "H2" ? 12f : 0f));
        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 120f,
                DerivedColumnCount = 2,
                ColumnWidths = [60f, 60f],
                Height = 40f,
                Rows =
                [
                    new TableLayoutRowResult
                    {
                        SourceRow = tableRow,
                        RowIndex = 0,
                        Y = 0f,
                        Height = 40f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = leftCell,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 60f,
                                Height = 40f
                            },
                            new TableLayoutCellPlacement
                            {
                                SourceCell = rightCell,
                                ColumnIndex = 1,
                                X = 60f,
                                Y = 0f,
                                Width = 60f,
                                Height = 40f
                            }
                        ]
                    }
                ]
            });

        var section = new BlockBox(DisplayRole.Block)
        {
            Style = new ComputedStyle()
        };
        section.Children.Add(new BlockBox(DisplayRole.Block)
        {
            Parent = section,
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "H2"),
            Style = new ComputedStyle()
        });
        section.Children.Add(new TableBox(DisplayRole.Table)
        {
            Parent = section,
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "TABLE"),
            Style = new ComputedStyle()
        });
        var table = (TableBox)section.Children[1];
        table.Children.Add(tableRow);

        var root = new BlockBox(DisplayRole.Block)
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
        laidOutTable.Role.ShouldBe(DisplayRole.Table);
        laidOutTable.Y.ShouldBe(12f);
        laidOutTable.Height.ShouldBe(40f);
        var laidOutRow = laidOutTable.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        laidOutRow.ShouldBeSameAs(tableRow);
        laidOutRow.Role.ShouldBe(DisplayRole.TableRow);
    }

    [Fact]
    public void Layout_TableGeometryFromTableLayoutEngine_IsMaterializedWithoutRecalculation()
    {
        var rowSource = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() };
        var cellSource = new TableCellBox(DisplayRole.TableCell)
        {
            Parent = rowSource,
            Style = new ComputedStyle
            {
                Padding = new Spacing(7.5f, 7.5f, 7.5f, 7.5f)
            }
        };
        rowSource.Children.Add(cellSource);

        _inlineEngine
            .Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>()))
            .Returns(CreateInlineLayoutResult(999f));
        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 120f,
                DerivedColumnCount = 1,
                ColumnWidths = [120f],
                Height = 29.5f,
                Rows =
                [
                    new TableLayoutRowResult
                    {
                        SourceRow = rowSource,
                        RowIndex = 0,
                        Y = 0f,
                        Height = 29.5f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = cellSource,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 120f,
                                Height = 29.5f
                            }
                        ]
                    }
                ]
            });

        var root = new TableBox(DisplayRole.Table)
        {
            Style = new ComputedStyle()
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
        _inlineEngine
            .Setup(x => x.Layout(It.IsAny<BlockBox>(), It.IsAny<InlineLayoutRequest>()))
            .Returns(CreateInlineLayoutResult(0f));

        var sourceRow = new TableRowBox(DisplayRole.TableRow)
        {
            Style = new ComputedStyle()
        };
        var sourceCell = new TableCellBox(DisplayRole.TableCell)
        {
            Parent = sourceRow,
            Style = new ComputedStyle()
        };
        sourceCell.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = sourceCell,
            TextContent = "alpha",
            Style = new ComputedStyle()
        });

        var nestedBlock = new BlockBox(DisplayRole.Block)
        {
            Parent = sourceCell,
            Style = new ComputedStyle()
        };
        nestedBlock.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            Parent = nestedBlock,
            TextContent = "beta",
            Style = new ComputedStyle()
        });
        sourceCell.Children.Add(nestedBlock);
        sourceRow.Children.Add(sourceCell);

        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(new TableLayoutResult
            {
                ResolvedWidth = 120f,
                DerivedColumnCount = 1,
                ColumnWidths = [120f],
                Height = 30f,
                Rows =
                [
                    new TableLayoutRowResult
                    {
                        SourceRow = sourceRow,
                        RowIndex = 0,
                        Y = 0f,
                        Height = 30f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = sourceCell,
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 120f,
                                Height = 30f
                            }
                        ]
                    }
                ]
            });

        var root = new TableBox(DisplayRole.Table)
        {
            Style = new ComputedStyle()
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
    public void Layout_UnsupportedTable_EmitsUnsupportedStructureDiagnosticsAndSkipsRows()
    {
        var diagnosticsSession = new DiagnosticsSession();
        _tableLayoutEngine
            .Setup(x => x.Layout(It.IsAny<TableBox>(), It.IsAny<float>()))
            .Returns(TableLayoutResult.Unsupported(
                requestedWidth: 150f,
                resolvedWidth: 150f,
                structureKind: HtmlCssConstants.HtmlAttributes.Colspan,
                reason: "Table cell colspan is not supported.",
                rowCount: 1));

        var root = new TableBox(DisplayRole.Table)
        {
            Element = Mock.Of<AngleSharp.Dom.IElement>(e => e.TagName == "TABLE"),
            Style = new ComputedStyle
            {
                WidthPt = 150f
            }
        };

        var result = new BlockLayoutEngine(
            _inlineEngine.Object,
            _tableLayoutEngine.Object,
            _floatLayoutEngine.Object,
            diagnosticsSession).Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(DisplayRole.Table);
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
    
    private BlockLayoutEngine CreateBlockLayoutEngine() => 
        new(_inlineEngine.Object, _tableLayoutEngine.Object, _floatLayoutEngine.Object);

    private static InlineLayoutResult CreateInlineLayoutResult(float totalHeight)
    {
        return new InlineLayoutResult([], totalHeight, 0f);
    }

    private static void NormalizeForBlockLayout(DisplayNode root)
    {
        if (root is BlockBox block)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(block);
        }
    }
}
