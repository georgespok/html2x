using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.BlockFlow;
using Html2x.LayoutEngine.Geometry.Images;
using Html2x.LayoutEngine.Geometry.InlineFlow;
using Html2x.LayoutEngine.Geometry.Measurement;
using Html2x.RenderModel.Styles;
using Html2x.Text;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test;

public class BlockBoxLayoutTableTests
{
    private readonly ITextMeasurer _textMeasurer = new FakeTextMeasurer(1f, 9f, 3f);

    private static PageBox DefaultPage() => new()
    {
        Margin = new(0, 0, 0, 0),
        Size = new(200, 400)
    };

    [Fact]
    public void Layout_TableNode_ProducesNestedTableRowAndCellBlocks()
    {
        var root = TableBoxTree.Create(120f);
        var firstRowSource = TableBoxTree.AddRow(root);
        var firstCellSource = TableBoxTree.AddCell(firstRowSource);
        var secondCellSource = TableBoxTree.AddCell(firstRowSource);
        var secondRowSource = TableBoxTree.AddRow(root);
        var thirdCellSource = TableBoxTree.AddCell(secondRowSource);

        var result = LayoutMutableBlocks(root);

        var table = result.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(BoxRole.Table);
        table.UsedGeometry.ShouldNotBeNull().Width.ShouldBe(120f);
        table.UsedGeometry.Value.Height.ShouldBe(40f);
        table.Children.Count.ShouldBe(2);

        var firstRow = table.Children[0].ShouldBeOfType<TableRowBox>();
        firstRow.ShouldBeSameAs(firstRowSource);
        firstRow.Role.ShouldBe(BoxRole.TableRow);
        firstRow.UsedGeometry.ShouldNotBeNull().Y.ShouldBe(0f);
        firstRow.Children.Count.ShouldBe(2);

        var firstCell = firstRow.Children[0].ShouldBeOfType<TableCellBox>();
        firstCell.ShouldBeSameAs(firstCellSource);
        firstCell.Role.ShouldBe(BoxRole.TableCell);
        firstCell.UsedGeometry.ShouldNotBeNull().X.ShouldBe(0f);
        firstCell.UsedGeometry.Value.Width.ShouldBe(60f);

        var secondCell = firstRow.Children[1].ShouldBeOfType<TableCellBox>();
        secondCell.ShouldBeSameAs(secondCellSource);
        secondCell.UsedGeometry.ShouldNotBeNull().X.ShouldBe(60f);
        secondCell.UsedGeometry.Value.Width.ShouldBe(60f);

        var secondRow = table.Children[1].ShouldBeOfType<TableRowBox>();
        secondRow.ShouldBeSameAs(secondRowSource);
        secondRow.Role.ShouldBe(BoxRole.TableRow);
        secondRow.UsedGeometry.ShouldNotBeNull().Y.ShouldBe(20f);
        var thirdCell = secondRow.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();
        thirdCell.ShouldBeSameAs(thirdCellSource);
        thirdCell.Role.ShouldBe(BoxRole.TableCell);
    }

    [Fact]
    public void Layout_TableMaterialization_PopulatesRowAndCellGeometry()
    {
        var root = TableBoxTree.Create(120f);
        var rowSource = TableBoxTree.AddRow(
            root,
            style: new()
            {
                Padding = new(1f, 2f, 3f, 4f)
            });
        TableBoxTree.AddCell(
            rowSource,
            new()
            {
                Padding = new(2f, 4f, 6f, 8f)
            });

        var result = LayoutMutableBlocks(root);

        var table = result.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        table.UsedGeometry.ShouldNotBeNull();
        row.UsedGeometry.ShouldNotBeNull();
        cell.UsedGeometry.ShouldNotBeNull();

        var rowGeometry = row.UsedGeometry!.Value;
        var cellGeometry = cell.UsedGeometry!.Value;
        rowGeometry.BorderBoxRect.X.ShouldBe(0f);
        rowGeometry.BorderBoxRect.Y.ShouldBe(0f);
        rowGeometry.BorderBoxRect.Width.ShouldBe(120f);
        rowGeometry.ContentBoxRect.X.ShouldBe(rowGeometry.X + 4f);
        rowGeometry.ContentBoxRect.Y.ShouldBe(rowGeometry.Y + 1f);
        cellGeometry.BorderBoxRect.X.ShouldBe(4f);
        cellGeometry.BorderBoxRect.Y.ShouldBe(1f);
        cellGeometry.BorderBoxRect.Width.ShouldBe(114f);
        cellGeometry.ContentBoxRect.X.ShouldBe(cellGeometry.X + 8f);
        cellGeometry.ContentBoxRect.Y.ShouldBe(cellGeometry.Y + 2f);
    }

    [Fact]
    public void Layout_TablePaddingAndBorder_PlacesCellsInContentBox()
    {
        var root = TableBoxTree.CreateWithStyle(
            new()
            {
                WidthPt = 104f,
                Padding = new(4f, 5f, 6f, 7f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            });
        TableBoxTree.AddRow(root, 1);

        var result = LayoutMutableBlocks(root);

        var table = result.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        table.UsedGeometry.ShouldNotBeNull().Width.ShouldBe(120f);
        table.UsedGeometry.Value.Height.ShouldBe(34f);
        table.UsedGeometry!.Value.ContentBoxRect.ShouldBe(new(9f, 6f, 104f, 20f));
        row.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new(9f, 6f, 104f, 20f));
        cell.UsedGeometry!.Value.BorderBoxRect.ShouldBe(new(9f, 6f, 104f, 20f));
    }

    [Fact]
    public void Layout_TableCellContent_PlacesNestedBlockAtCellContentBox()
    {
        var root = TableBoxTree.Create(120f);
        var sourceRow = TableBoxTree.AddRow(root);
        var sourceCell = TableBoxTree.AddCell(
            sourceRow,
            new()
            {
                Padding = new(3f, 4f, 5f, 6f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            });
        var nestedBlock = TableBoxTree.AddBlock(
            sourceCell,
            new()
            {
                HeightPt = 7f
            });

        var result = LayoutMutableBlocks(root);

        var cell = result
            .ShouldHaveSingleItem().ShouldBeOfType<TableBox>()
            .Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>()
            .Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();
        var cellContent = cell.UsedGeometry.ShouldNotBeNull().ContentBoxRect;
        var laidOutNestedBlock = cell.Children.ShouldHaveSingleItem().ShouldBeOfType<BlockBox>();

        laidOutNestedBlock.ShouldBeSameAs(nestedBlock);
        var nestedGeometry = laidOutNestedBlock.UsedGeometry.ShouldNotBeNull();
        nestedGeometry.X.ShouldBe(cellContent.X);
        nestedGeometry.Y.ShouldBe(cellContent.Y);
        nestedGeometry.Width.ShouldBe(cellContent.Width);
    }

    [Fact]
    public void Layout_BlockContainerWithNestedTable_PreservesTableInChildFlow()
    {
        var section = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        var headingSource = new BlockBox(BoxRole.Block)
        {
            Parent = section,
            Element = StyledElementFacts.Create(HtmlCssVocabulary.HtmlTags.H2),
            Style = new()
            {
                HeightPt = 12f
            }
        };
        headingSource.AddChild(new InlineBox(BoxRole.Inline)
        {
            Parent = headingSource,
            Style = new(),
            TextContent = "Heading"
        });
        section.AddChild(headingSource);

        var table = TableBoxTree.AddTable(section, 120f);
        var tableRow = TableBoxTree.AddRow(table);
        var leftCell = TableBoxTree.AddCell(tableRow);
        TableBoxTree.AddCell(tableRow);
        TableBoxTree.AddBlock(
            leftCell,
            new()
            {
                HeightPt = 40f
            });

        var root = new BlockBox(BoxRole.Block)
        {
            Style = new()
        };
        root.AddChild(section);

        var result = LayoutMutableBlocks(root);

        var laidOutSection = result.ShouldHaveSingleItem();
        laidOutSection.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(52f);
        laidOutSection.Children.Count.ShouldBe(2);

        var heading = laidOutSection.Children[0].ShouldBeOfType<BlockBox>();
        heading.UsedGeometry.ShouldNotBeNull().Y.ShouldBe(0f);
        heading.UsedGeometry.Value.Height.ShouldBe(12f);

        var laidOutTable = laidOutSection.Children[1].ShouldBeOfType<TableBox>();
        laidOutTable.Role.ShouldBe(BoxRole.Table);
        laidOutTable.UsedGeometry.ShouldNotBeNull().Y.ShouldBe(12f);
        laidOutTable.UsedGeometry.Value.Height.ShouldBe(40f);
        var laidOutRow = laidOutTable.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        laidOutRow.ShouldBeSameAs(tableRow);
        laidOutRow.Role.ShouldBe(BoxRole.TableRow);
    }

    [Fact]
    public void Layout_TableGeometry_MaterializesWithoutRecalculation()
    {
        var root = TableBoxTree.Create(120f);
        var rowSource = TableBoxTree.AddRow(root);
        var cellSource = TableBoxTree.AddCell(
            rowSource,
            new()
            {
                Padding = new(7.5f, 7.5f, 7.5f, 7.5f)
            });
        TableBoxTree.AddBlock(
            cellSource,
            new()
            {
                HeightPt = 14.5f
            });

        var result = LayoutMutableBlocks(root);

        var table = result.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        var row = table.Children.ShouldHaveSingleItem().ShouldBeOfType<TableRowBox>();
        var cell = row.Children.ShouldHaveSingleItem().ShouldBeOfType<TableCellBox>();

        row.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(29.5f);
        cell.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(29.5f);
        table.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(29.5f);
    }

    [Fact]
    public void Layout_TableCellContent_IsMappedIntoMaterializedCellTree()
    {
        var root = TableBoxTree.Create(120f);
        var sourceRow = TableBoxTree.AddRow(root);
        var sourceCell = TableBoxTree.AddCell(sourceRow);
        TableBoxTree.AddInline(sourceCell, "alpha", new());

        var nestedBlock = TableBoxTree.AddBlock(sourceCell);
        TableBoxTree.AddInline(nestedBlock, "beta", new());

        var result = LayoutMutableBlocks(root);

        var cell = result
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
        var diagnosticsSink = new RecordingDiagnosticsSink();
        var root = TableBoxTree.Create(150f);
        var row = TableBoxTree.AddRow(root);
        TableBoxTree.AddCell(row, rowspan: "2");

        var result = LayoutMutableBlocks(root, diagnosticsSink);

        var table = result.ShouldHaveSingleItem().ShouldBeOfType<TableBox>();
        table.Role.ShouldBe(BoxRole.Table);
        table.UsedGeometry.ShouldNotBeNull().Height.ShouldBe(0f);
        table.Children.ShouldBeEmpty();
        diagnosticsSink.Records.Any(e =>
                e.Name == "layout/table/unsupported-structure" &&
                e.Fields["structureKind"] is DiagnosticStringValue { Value: HtmlCssVocabulary.HtmlAttributes.Rowspan } &&
                e.Fields["reason"] is DiagnosticStringValue { Value: "Table cell rowspan is not supported." })
            .ShouldBeTrue();
        diagnosticsSink.Records.Any(e =>
                e.Name == "layout/table" &&
                e.Fields["outcome"] is DiagnosticStringValue { Value: "Unsupported" } &&
                e.Fields["rowCount"] is DiagnosticNumberValue { Value: 1 } &&
                e.Fields["reason"] is DiagnosticStringValue { Value: "Table cell rowspan is not supported." })
            .ShouldBeTrue();
    }

    private BlockBoxLayout CreateBlockBoxLayout(IDiagnosticsSink? diagnosticsSink = null)
    {
        var formattingContext = new BlockFormattingMetricsMeasurement();
        var imageSizingRules = new ImageSizingRules();
        var inlineFlowLayout = new InlineFlowLayout(
            new DefaultFontMetricsMeasurer(),
            _textMeasurer,
            new LineHeightRules(),
            formattingContext,
            imageSizingRules,
            diagnosticsSink);
        return new(
            inlineFlowLayout,
            new(inlineFlowLayout, imageSizingRules),
            formattingContext,
            imageSizingRules,
            diagnosticsSink);
    }

    private IReadOnlyList<BlockBox> LayoutMutableBlocks(BoxNode root, IDiagnosticsSink? diagnosticsSink = null)
    {
        var page = DefaultPage();
        _ = PublishedLayoutTestRunner.Run(CreateBlockBoxLayout(diagnosticsSink), root, page);

        if (root is TableBox tableRoot)
        {
            return [tableRoot];
        }

        if (root is BlockBox rootBlock)
        {
            return rootBlock.Children.OfType<BlockBox>().ToList();
        }

        return [];
    }
}
