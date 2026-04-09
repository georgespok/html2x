using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Html2x.Abstractions.Diagnostics;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;

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
    }
    
    [Fact]
    public void Layout_BlockHeightIncludesPadding()
    {
        // Arrange
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(24f);

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
    public void Layout_MixedInlineAndBlock_ProducesAnonymousBlockForInlineRun()
    {
        // Arrange
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(10f);

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
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(10f);

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
        _inlineEngine.Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>())).Returns(12f);

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
                        SourceRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() },
                        RowIndex = 0,
                        Y = 0f,
                        Height = 20f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell) { Style = new ComputedStyle() },
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 60f,
                                Height = 20f
                            },
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell) { Style = new ComputedStyle() },
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
                        SourceRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() },
                        RowIndex = 1,
                        Y = 20f,
                        Height = 20f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell) { Style = new ComputedStyle() },
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

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var table = result.Blocks.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(DisplayRole.Table);
        table.Width.ShouldBe(120f);
        table.Height.ShouldBe(40f);
        table.Children.Count.ShouldBe(2);

        var firstRow = table.Children[0].ShouldBeOfType<TableRowBox>();
        firstRow.Role.ShouldBe(DisplayRole.TableRow);
        firstRow.Y.ShouldBe(0f);
        firstRow.Children.Count.ShouldBe(2);

        var firstCell = firstRow.Children[0].ShouldBeOfType<TableCellBox>();
        firstCell.Role.ShouldBe(DisplayRole.TableCell);
        firstCell.X.ShouldBe(0f);
        firstCell.Width.ShouldBe(60f);

        var secondCell = firstRow.Children[1].ShouldBeOfType<TableCellBox>();
        secondCell.X.ShouldBe(60f);
        secondCell.Width.ShouldBe(60f);

        var secondRow = table.Children[1].ShouldBeOfType<TableRowBox>();
        secondRow.Role.ShouldBe(DisplayRole.TableRow);
        secondRow.Y.ShouldBe(20f);
        secondRow.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>().Role.ShouldBe(DisplayRole.TableCell);
    }

    [Fact]
    public void Layout_BlockContainerWithNestedTable_PreservesTableInChildFlow()
    {
        _inlineEngine
            .Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>()))
            .Returns<DisplayNode, float>((node, _) => node.Element?.TagName == "H2" ? 12f : 0f);
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
                        SourceRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() },
                        RowIndex = 0,
                        Y = 0f,
                        Height = 40f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell) { Style = new ComputedStyle() },
                                ColumnIndex = 0,
                                X = 0f,
                                Y = 0f,
                                Width = 60f,
                                Height = 40f
                            },
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell) { Style = new ComputedStyle() },
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

        var table = laidOutSection.Children[1].ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(DisplayRole.Table);
        table.Y.ShouldBe(12f);
        table.Height.ShouldBe(40f);
        table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>().Role.ShouldBe(DisplayRole.TableRow);
    }

    [Fact]
    public void Layout_TableGeometryFromTableLayoutEngine_IsMaterializedWithoutRecalculation()
    {
        _inlineEngine
            .Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>()))
            .Returns(999f);
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
                        SourceRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() },
                        RowIndex = 0,
                        Y = 0f,
                        Height = 29.5f,
                        Cells =
                        [
                            new TableLayoutCellPlacement
                            {
                                SourceCell = new TableCellBox(DisplayRole.TableCell)
                                {
                                    Style = new ComputedStyle
                                    {
                                        Padding = new Spacing(7.5f, 7.5f, 7.5f, 7.5f)
                                    }
                                },
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
            .Setup(x => x.MeasureHeight(It.IsAny<DisplayNode>(), It.IsAny<float>()))
            .Returns(0f);

        var sourceCell = new TableCellBox(DisplayRole.TableCell)
        {
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
                        SourceRow = new TableRowBox(DisplayRole.TableRow) { Style = new ComputedStyle() },
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

        var result = CreateBlockLayoutEngine().Layout(root, DefaultPage());

        var cell = result.Blocks
            .ShouldHaveSingleItem().ShouldBeOfType<TableBox>()
            .Children.ShouldHaveSingleItem()
            .ShouldBeOfType<TableRowBox>()
            .Children.ShouldHaveSingleItem()
            .ShouldBeOfType<TableCellBox>();

        var inlineChild = cell.Children[0].ShouldBeOfType<InlineBox>();
        inlineChild.TextContent.ShouldBe("alpha");

        var mappedNestedBlock = cell.Children[1].ShouldBeOfType<BlockBox>();
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

    private static void NormalizeForBlockLayout(DisplayNode root)
    {
        if (root is BlockBox block)
        {
            BlockFlowNormalization.NormalizeChildrenForBlock(block);
        }
    }
}
