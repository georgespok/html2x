using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Geometry.Tables;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test;

public class TableGridLayoutTests
{
    [Fact]
    public void Layout_ExplicitWidthAndTwoCells_ResolvesRequestedTableWidth()
    {
        var table = TableBoxTree.Create(400f, 2);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedBorderBoxWidth.ShouldBe(400f);
    }

    [Fact]
    public void Layout_WidestRowDefinesDerivedColumnCount()
    {
        var table = TableBoxTree.Create(400f, 2, 3);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
    }

    [Fact]
    public void Layout_EqualWidthDistribution_SplitsResolvedBorderBoxWidthAcrossDerivedColumns()
    {
        var table = TableBoxTree.Create(400f, 2);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.ColumnWidths.ShouldBe([200f, 200f]);
    }

    [Fact]
    public void Layout_TablePaddingAndBorder_SplitsContentWidthAcrossDerivedColumns()
    {
        var table = TableBoxTree.CreateWithStyle(
            new()
            {
                WidthPt = 120f,
                Padding = new(0f, 10f, 0f, 10f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            },
            2);

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedBorderBoxWidth.ShouldBe(144f);
        result.ColumnWidths.ShouldBe([60f, 60f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(120f);
        result.Rows[0].Cells[1].UsedGeometry.X.ShouldBe(60f);
    }

    [Fact]
    public void Layout_TableSizing_UsesBlockMeasurementPolicy()
    {
        var table = TableBoxTree.CreateWithStyle(
            new()
            {
                Margin = new(0f, 20f, 0f, 10f),
                MinWidthPt = 120f,
                MaxWidthPt = 150f,
                Padding = new(0f, 5f, 0f, 5f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            },
            2);
        table.MarkerOffset = 7f;

        var result = Layout(table, 100f);

        result.ResolvedBorderBoxWidth.ShouldBe(134f);
        result.ColumnWidths.ShouldBe([56.5f, 56.5f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(113f);
    }

    [Fact]
    public void Layout_TableExplicitWidth_ClampsToMaxWidth()
    {
        var table = TableBoxTree.Create(220f, 2);
        table.Style = table.Style with
        {
            MaxWidthPt = 150f
        };

        var result = Layout(table, 300f);

        result.ResolvedBorderBoxWidth.ShouldBe(150f);
        result.ColumnWidths.ShouldBe([75f, 75f]);
    }

    [Fact]
    public void Layout_RowPaddingAndBorder_PlacesCellsInsideRowContentBox()
    {
        var table = TableBoxTree.Create(120f);
        TableBoxTree.AddRow(
            table,
            1,
            new()
            {
                Padding = new(3f, 0f, 0f, 6f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            });

        var result = Layout(table, 200f);

        var rowResult = result.Rows.ShouldHaveSingleItem();
        var rowContent = rowResult.UsedGeometry.ContentBoxRect;
        var cellGeometry = rowResult.Cells.ShouldHaveSingleItem().UsedGeometry;

        cellGeometry.BorderBoxRect.Left.ShouldBeGreaterThanOrEqualTo(rowContent.Left);
        cellGeometry.BorderBoxRect.Top.ShouldBeGreaterThanOrEqualTo(rowContent.Top);
        cellGeometry.BorderBoxRect.Right.ShouldBeLessThanOrEqualTo(rowContent.Right);
        cellGeometry.BorderBoxRect.Bottom.ShouldBeLessThanOrEqualTo(rowContent.Bottom);
    }

    [Fact]
    public void Layout_CellPaddingAndBorder_ExposesContentBoxInsideCellPlacement()
    {
        var table = TableBoxTree.Create(120f);
        var row = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(row, new()
        {
            Padding = new(3f, 4f, 5f, 6f),
            Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
        });

        var result = Layout(table, 200f);

        var placement = result.Rows.ShouldHaveSingleItem().Cells.ShouldHaveSingleItem();
        var geometry = placement.UsedGeometry;

        geometry.BorderBoxRect.ShouldBe(new(0f, 0f, 120f, placement.UsedGeometry.Height));
        geometry.ContentBoxRect.X.ShouldBe(8f);
        geometry.ContentBoxRect.Y.ShouldBe(5f);
        geometry.ContentBoxRect.Width.ShouldBe(106f);
        geometry.ContentBoxRect.Height.ShouldBe(placement.UsedGeometry.Height - 12f);
    }

    [Fact]
    public void TableLayoutCellPlacement_StoresCanonicalGeometry()
    {
        var cell = new TableCellBox(BoxRole.TableCell)
        {
            Style = new()
        };

        var placement = new TableLayoutCellPlacement(
            cell,
            0,
            1,
            false,
            UsedGeometryRules.FromBorderBox(
                new(0f, 0f, 10f, 10f),
                new(),
                new()));

        placement.UsedGeometry.BorderBoxRect.ShouldBe(new(0f, 0f, 10f, 10f));
        placement.ColumnSpan.ShouldBe(1);
    }

    [Fact]
    public void WriteSupported_TranslatesRowAndCellGeometryThroughGeometryOwner()
    {
        var table = TableBoxTree.Create();
        var row = TableBoxTree.AddRow(table);
        var cell = TableBoxTree.AddCell(row);
        var rowGeometry = UsedGeometryRules.FromBorderBox(
            0f,
            1f,
            50f,
            20f,
            new(1f, 2f, 3f, 4f),
            new(1f, 1f, 1f, 1f),
            12f,
            5f);
        var cellGeometry = UsedGeometryRules.FromBorderBox(
            2f,
            3f,
            25f,
            10f,
            new(1f, 1f, 1f, 1f),
            new(),
            8f,
            4f);
        var result = new TableLayoutResult
        {
            IsSupported = true,
            ResolvedBorderBoxWidth = 50f,
            DerivedColumnCount = 1,
            Rows =
            [
                new(
                    row,
                    0,
                    rowGeometry,
                    [new(cell, 0, 2, false, cellGeometry)])
            ],
            ContentHeight = 20f,
            BorderBoxHeight = 20f
        };

        _ = new TablePlacementWriter().WriteSupported(
            table,
            result,
            30f,
            40f,
            new(),
            static _ => 0f);

        var appliedRow = row.UsedGeometry.ShouldNotBeNull();
        var appliedCell = cell.UsedGeometry.ShouldNotBeNull();

        appliedRow.BorderBoxRect.ShouldBe(new(30f, 41f, 50f, 20f));
        appliedRow.ContentBoxRect.ShouldBe(new(35f, 43f, 42f, 14f));
        appliedRow.Baseline.ShouldBe(52f);
        appliedRow.MarkerOffset.ShouldBe(5f);
        appliedCell.BorderBoxRect.ShouldBe(new(32f, 43f, 25f, 10f));
        appliedCell.ContentBoxRect.ShouldBe(new(33f, 44f, 23f, 8f));
        appliedCell.Baseline.ShouldBe(48f);
        appliedCell.MarkerOffset.ShouldBe(4f);
        cell.ColumnSpan.ShouldBe(2);
    }

    [Fact]
    public void Layout_ShorterRows_DoNotChangeSharedColumnGrid()
    {
        var table = TableBoxTree.Create(300f, 3, 2);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
        result.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void Layout_ColspanContributesToDerivedColumnCountAndCellWidth()
    {
        var table = TableBoxTree.Create(240f);
        var spannedRow = TableBoxTree.AddRow(table);
        var spannedCell = TableBoxTree.AddCell(spannedRow, colspan: "2");
        var bodyRow = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(bodyRow);
        TableBoxTree.AddCell(bodyRow);

        var result = Layout(table, 300f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(2);
        result.ColumnWidths.ShouldBe([120f, 120f]);

        var firstRowCell = result.Rows[0].Cells.ShouldHaveSingleItem();
        firstRowCell.SourceCell.ShouldBeSameAs(spannedCell);
        firstRowCell.ColumnIndex.ShouldBe(0);
        firstRowCell.ColumnSpan.ShouldBe(2);
        firstRowCell.UsedGeometry.BorderBoxRect.ShouldBe(new(0f, 0f, 240f, firstRowCell.UsedGeometry.Height));

        result.Rows[1].Cells.Select(static cell => cell.ColumnIndex).ShouldBe([0, 1]);
        result.Rows[1].Cells.Select(static cell => cell.ColumnSpan).ShouldBe([1, 1]);
        result.Rows[1].Cells.Select(static cell => cell.UsedGeometry.Width).ShouldBe([120f, 120f]);
    }

    [Fact]
    public void Layout_HeaderColspan_PreservesHeaderIdentityAndSpan()
    {
        var table = TableBoxTree.Create(300f);
        var headerRow = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(headerRow, isHeader: true, colspan: "3");
        TableBoxTree.AddRow(table, 3);

        var result = Layout(table, 400f);

        var placement = result.Rows[0].Cells.ShouldHaveSingleItem();
        placement.IsHeader.ShouldBeTrue();
        placement.ColumnIndex.ShouldBe(0);
        placement.ColumnSpan.ShouldBe(3);
        placement.UsedGeometry.Width.ShouldBe(300f);
    }

    [Fact]
    public void Layout_SharedColumnGrid_DrivesEveryRowPlacement()
    {
        var table = TableBoxTree.Create(300f, 3, 2);

        var result = Layout(table, 500f);

        result.ColumnWidths.ShouldBe([100f, 100f, 100f]);
        var secondRow = result.Rows[1];
        secondRow.Cells.Count.ShouldBe(2);
        secondRow.Cells[0].UsedGeometry.BorderBoxRect
            .ShouldBe(new(0f, secondRow.UsedGeometry.Y, 100f, secondRow.UsedGeometry.Height));
        secondRow.Cells[1].UsedGeometry.BorderBoxRect
            .ShouldBe(new(100f, secondRow.UsedGeometry.Y, 100f, secondRow.UsedGeometry.Height));
    }

    [Theory]
    [InlineData(HtmlCssVocabulary.HtmlAttributes.Colspan)]
    [InlineData(HtmlCssVocabulary.HtmlAttributes.Rowspan)]
    public void Layout_SpanOne_RemainsSupportedAndUsesCurrentGridFacts(string attributeName)
    {
        var table = TableBoxTree.Create(120f);
        var row = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(
            row,
            colspan: attributeName == HtmlCssVocabulary.HtmlAttributes.Colspan ? "1" : null,
            rowspan: attributeName == HtmlCssVocabulary.HtmlAttributes.Rowspan ? "1" : null);
        TableBoxTree.AddCell(row);

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(2);
        result.ColumnWidths.ShouldBe([60f, 60f]);
        result.Rows.ShouldHaveSingleItem().Cells.Count.ShouldBe(2);
        result.Rows.ShouldHaveSingleItem().Cells.Select(static cell => cell.ColumnSpan).ShouldBe([1, 1]);
    }

    [Theory]
    [InlineData(HtmlCssVocabulary.HtmlAttributes.Rowspan, "2")]
    [InlineData(HtmlCssVocabulary.HtmlAttributes.Colspan, "invalid")]
    [InlineData(HtmlCssVocabulary.HtmlAttributes.Rowspan, "0")]
    public void Layout_UnsupportedSpans_StopBeforeGridFacts(string attributeName, string value)
    {
        var table = TableBoxTree.Create(120f);
        var row = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(
            row,
            colspan: attributeName == HtmlCssVocabulary.HtmlAttributes.Colspan ? value : null,
            rowspan: attributeName == HtmlCssVocabulary.HtmlAttributes.Rowspan ? value : null);

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe(attributeName);
        result.RowCount.ShouldBe(1);
        result.DerivedColumnCount.ShouldBe(0);
        result.ColumnWidths.ShouldBeEmpty();
        result.Rows.ShouldBeEmpty();
        result.ContentHeight.ShouldBe(0f);
        result.BorderBoxHeight.ShouldBe(0f);
    }

    [Fact]
    public void Layout_HeaderCells_PreserveHeaderIdentityInCellPlacements()
    {
        var table = TableBoxTree.Create(200f);
        var headerRow = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(headerRow, isHeader: true);
        TableBoxTree.AddRow(table, 1);

        var result = Layout(table, 300f);

        result.IsSupported.ShouldBeTrue();
        result.Rows[0].Cells[0].IsHeader.ShouldBeTrue();
        result.Rows[1].Cells[0].IsHeader.ShouldBeFalse();
    }

    [Fact]
    public void Layout_GenericContainerInsideTable_ReturnsUnsupportedResult()
    {
        var table = TableBoxTree.Create(400f);
        var section = new InlineBox(BoxRole.Inline)
        {
            Parent = table,
            Style = new()
        };
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Parent = section,
            Style = new()
        };
        row.AddChild(new TableCellBox(BoxRole.TableCell) { Parent = row, Style = new() });
        row.AddChild(new TableCellBox(BoxRole.TableCell) { Parent = row, Style = new() });
        section.AddChild(row);
        table.AddChild(section);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe("unsupported-table-child");
        result.UnsupportedReason.ShouldNotBeNull();
        result.UnsupportedReason.ShouldContain("direct row and section children");
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Layout_SectionedTable_PreservesRowOrderAndSequentialOffsets()
    {
        var table = TableBoxTree.Create(400f);
        var section = TableBoxTree.AddSection(table);
        var firstRow = TableBoxTree.AddRow(section, 2);
        var secondRow = TableBoxTree.AddRow(section, 2);

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
        result.DerivedColumnCount.ShouldBe(2);
        result.Rows[0].SourceRow.ShouldBeSameAs(firstRow);
        result.Rows[1].SourceRow.ShouldBeSameAs(secondRow);
        result.Rows[0].UsedGeometry.Y.ShouldBe(0f);
        result.Rows[1].UsedGeometry.Y.ShouldBe(result.Rows[0].UsedGeometry.Height, 0.01f);
    }

    [Fact]
    public void Layout_NestedTableInsideCell_DoesNotLeakInnerRowsIntoOuterGrid()
    {
        var outerTable = TableBoxTree.Create(300f);
        var outerRow = TableBoxTree.AddRow(outerTable);
        var outerCell = TableBoxTree.AddCell(outerRow);
        TableBoxTree.AddTable(outerCell, 120f, 1);
        TableBoxTree.AddCell(outerRow);

        var result = Layout(outerTable, 500f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.DerivedColumnCount.ShouldBe(2);
        result.Rows[0].Cells.Count.ShouldBe(2);
    }

    [Fact]
    public void Layout_UnsupportedRowspan_ReturnsUnsupportedBeforeGeometry()
    {
        var table = TableBoxTree.Create(120f);
        var row = TableBoxTree.AddRow(table);
        TableBoxTree.AddCell(row, rowspan: "2");

        var result = Layout(table, 200f);
        var diagnosticsSink = new RecordingDiagnosticsSink();
        TableGridDiagnostics.EmitUnsupportedTable(
            "html/body/table",
            result.UnsupportedStructureKind ?? string.Empty,
            result.UnsupportedReason ?? string.Empty,
            result.RowCount,
            result.RequestedContentWidth,
            result.ResolvedBorderBoxWidth,
            diagnosticsSink: diagnosticsSink);

        result.IsSupported.ShouldBeFalse();
        result.RowCount.ShouldBe(1);
        result.UnsupportedStructureKind.ShouldBe(HtmlCssVocabulary.HtmlAttributes.Rowspan);
        result.UnsupportedReason.ShouldBe("Table cell rowspan is not supported.");
        result.Rows.ShouldBeEmpty();
        result.ContentHeight.ShouldBe(0f);
        result.BorderBoxHeight.ShouldBe(0f);

        var unsupportedRecord = diagnosticsSink.Records.Single(e => e.Name == "layout/table");
        var reason = unsupportedRecord.Fields["reason"].ShouldBeOfType<DiagnosticStringValue>().Value;
        unsupportedRecord.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Unsupported"));
        unsupportedRecord.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(1));
        reason.ShouldContain(HtmlCssVocabulary.HtmlAttributes.Rowspan);
    }

    [Fact]
    public void Layout_SectionContainingNestedSection_ReturnsUnsupportedResult()
    {
        var table = TableBoxTree.Create(120f);
        var outerSection = TableBoxTree.AddSection(table);
        var innerSection = new TableSectionBox(BoxRole.TableSection)
        {
            Parent = outerSection,
            Style = new()
        };
        TableBoxTree.AddRow(innerSection, 1);
        outerSection.AddChild(innerSection);

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe("malformed-section-nesting");
        result.UnsupportedReason.ShouldBe("Table sections cannot contain nested table sections.");
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Layout_TallestCellOwnsRowHeightAndCellPlacements()
    {
        var table = TableBoxTree.Create(120f);
        var sourceRow = TableBoxTree.AddRow(table);
        var paddedCell = TableBoxTree.AddCell(sourceRow, new()
        {
            Padding = new(7.5f, 7.5f, 7.5f, 7.5f),
            Borders = BorderEdges.Uniform(new(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
        });
        TableBoxTree.AddInline(paddedCell, "A");

        var defaultCell = TableBoxTree.AddCell(sourceRow);
        TableBoxTree.AddInline(defaultCell, "B");

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);

        var row = result.Rows.ShouldHaveSingleItem();
        var firstPlacement = row.Cells[0];
        var secondPlacement = row.Cells[1];

        row.UsedGeometry.Y.ShouldBe(0f);
        row.UsedGeometry.Height.ShouldBe(30.9f, 0.2f);
        firstPlacement.UsedGeometry.Y.ShouldBe(0f);
        firstPlacement.UsedGeometry.Height.ShouldBe(row.UsedGeometry.Height, 0.01f);
        secondPlacement.UsedGeometry.Y.ShouldBe(0f);
        secondPlacement.UsedGeometry.Height.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.ContentHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
    }

    [Fact]
    public void Layout_CellWithStackedBlockChildren_UsesSharedCollapsedMarginHeight()
    {
        var table = TableBoxTree.Create(120f);
        var sourceRow = TableBoxTree.AddRow(table);
        var cell = TableBoxTree.AddCell(sourceRow);
        TableBoxTree.AddBlock(cell, new()
        {
            HeightPt = 10f,
            Margin = new(0f, 0f, 12f, 0f)
        });
        TableBoxTree.AddBlock(cell, new()
        {
            HeightPt = 8f,
            Margin = new(4f, 0f, 0f, 0f)
        });

        var result = Layout(table, 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.UsedGeometry.Height.ShouldBe(30f, 0.01f);
        result.ContentHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
    }

    [Fact]
    public void Layout_CellWithNestedPaddedTable_UsesNestedTableBorderBoxHeight()
    {
        var outerTable = TableBoxTree.Create(120f);
        var outerRow = TableBoxTree.AddRow(outerTable);
        var outerCell = TableBoxTree.AddCell(outerRow);
        TableBoxTree.AddTableWithStyle(
            outerCell,
            new()
            {
                WidthPt = 80f,
                Padding = new(5f, 0f, 7f, 0f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            },
            1);

        var result = Layout(outerTable, 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.UsedGeometry.Height.ShouldBe(36f, 0.01f);
        result.ContentHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
    }

    private static TableLayoutResult Layout(TableBox table, float availableWidth) =>
        new TableGridLayout().Layout(table, availableWidth);
}
