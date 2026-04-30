using System.Drawing;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using Shouldly;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Test;

public class TableLayoutEngineTests
{
    [Fact]
    public void Layout_ExplicitWidthAndTwoCells_ResolvesRequestedTableWidth()
    {
        var table = CreateTable(
            widthPt: 400f,
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedWidth.ShouldBe(400f);
    }

    [Fact]
    public void Layout_WidestRowDefinesDerivedColumnCount()
    {
        var table = CreateTable(
            widthPt: 400f,
            CreateRow(
                CreateCell(),
                CreateCell()),
            CreateRow(
                CreateCell(),
                CreateCell(),
                CreateCell()));

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
    }

    [Fact]
    public void Layout_EqualWidthDistribution_SplitsResolvedWidthAcrossDerivedColumns()
    {
        var table = CreateTable(
            widthPt: 400f,
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.ColumnWidths.ShouldBe([200f, 200f]);
    }

    [Fact]
    public void Layout_TablePaddingAndBorder_SplitsContentWidthAcrossDerivedColumns()
    {
        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f,
                Padding = new Spacing(0f, 10f, 0f, 10f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        table.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var result = Layout(table, availableWidth: 200f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedWidth.ShouldBe(144f);
        result.ColumnWidths.ShouldBe([60f, 60f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(120f);
        result.Rows[0].Cells[1].X.ShouldBe(60f);
    }

    [Fact]
    public void Layout_TableSizing_UsesBlockMeasurementPolicy()
    {
        var table = new TableBox(BoxRole.Table)
        {
            MarkerOffset = 7f,
            Style = new ComputedStyle
            {
                Margin = new Spacing(0f, 20f, 0f, 10f),
                MinWidthPt = 120f,
                MaxWidthPt = 150f,
                Padding = new Spacing(0f, 5f, 0f, 5f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        table.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var result = Layout(table, availableWidth: 100f);

        result.ResolvedWidth.ShouldBe(134f);
        result.ColumnWidths.ShouldBe([56.5f, 56.5f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(113f);
    }

    [Fact]
    public void Layout_TableExplicitWidth_ClampsToMaxWidth()
    {
        var table = CreateTable(
            widthPt: 220f,
            CreateRow(
                CreateCell(),
                CreateCell()));
        table.Style = table.Style with
        {
            MaxWidthPt = 150f
        };

        var result = Layout(table, availableWidth: 300f);

        result.ResolvedWidth.ShouldBe(150f);
        result.ColumnWidths.ShouldBe([75f, 75f]);
    }

    [Fact]
    public void Layout_RowPaddingAndBorder_PlacesCellsInsideRowContentBox()
    {
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Style = new ComputedStyle
            {
                Padding = new Spacing(3f, 0f, 0f, 6f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        row.Children.Add(CreateCell());

        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        table.Children.Add(row);

        var result = Layout(table, availableWidth: 200f);

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
        var cell = CreateCell(new ComputedStyle
        {
            Padding = new Spacing(3f, 4f, 5f, 6f),
            Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
        });
        var table = CreateTable(widthPt: 120f, CreateRow(cell));

        var result = Layout(table, availableWidth: 200f);

        var placement = result.Rows.ShouldHaveSingleItem().Cells.ShouldHaveSingleItem();
        var geometry = placement.UsedGeometry;

        geometry.BorderBoxRect.ShouldBe(new RectangleF(0f, 0f, 120f, placement.Height));
        geometry.ContentBoxRect.X.ShouldBe(8f);
        geometry.ContentBoxRect.Y.ShouldBe(5f);
        geometry.ContentBoxRect.Width.ShouldBe(106f);
        geometry.ContentBoxRect.Height.ShouldBe(placement.Height - 12f);
    }

    [Fact]
    public void TableLayoutCellPlacement_GeometryReadsCanonicalValues()
    {
        var cell = CreateCell();

        var placement = new TableLayoutCellPlacement(
            cell,
            0,
            false,
            BoxGeometryFactory.FromBorderBox(
                new RectangleF(0f, 0f, 10f, 10f),
                new Spacing(),
                new Spacing()));

        placement.UsedGeometry.BorderBoxRect.ShouldBe(new RectangleF(0f, 0f, 10f, 10f));
        placement.X.ShouldBe(0f);
        placement.Y.ShouldBe(0f);
        placement.Width.ShouldBe(10f);
        placement.Height.ShouldBe(10f);
    }

    [Fact]
    public void Layout_ShorterRows_DoNotChangeSharedColumnGrid()
    {
        var table = CreateTable(
            widthPt: 300f,
            CreateRow(
                CreateCell(),
                CreateCell(),
                CreateCell()),
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
        result.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void Layout_HeaderCells_PreserveHeaderIdentityInCellPlacements()
    {
        var headerCell = CreateCell(element: CreateElement("TH"));
        var bodyCell = CreateCell(element: CreateElement("TD"));
        var table = CreateTable(
            widthPt: 200f,
            CreateRow(headerCell),
            CreateRow(bodyCell));

        var result = Layout(table, availableWidth: 300f);

        result.IsSupported.ShouldBeTrue();
        result.Rows[0].Cells[0].IsHeader.ShouldBeTrue();
        result.Rows[1].Cells[0].IsHeader.ShouldBeFalse();
    }

    [Fact]
    public void Layout_GenericContainerInsideTable_ReturnsUnsupportedResult()
    {
        var section = new InlineBox(BoxRole.Inline);
        section.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 400f
            }
        };
        table.Children.Add(section);

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe("unsupported-table-child");
        result.UnsupportedReason.ShouldNotBeNull();
        result.UnsupportedReason.ShouldContain("direct row and section children");
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Layout_SectionedTable_PreservesRowOrderAndSequentialOffsets()
    {
        var firstRow = CreateRow(
            CreateCell(),
            CreateCell());
        var secondRow = CreateRow(
            CreateCell(),
            CreateCell());
        var section = new TableSectionBox(BoxRole.TableSection);
        section.Children.Add(firstRow);
        section.Children.Add(secondRow);

        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 400f
            }
        };
        table.Children.Add(section);

        var result = Layout(table, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
        result.DerivedColumnCount.ShouldBe(2);
        result.Rows[0].SourceRow.ShouldBeSameAs(firstRow);
        result.Rows[1].SourceRow.ShouldBeSameAs(secondRow);
        result.Rows[0].Y.ShouldBe(0f);
        result.Rows[1].Y.ShouldBe(result.Rows[0].Height, 0.01f);
    }

    [Fact]
    public void Layout_NestedTableInsideCell_DoesNotLeakInnerRowsIntoOuterGrid()
    {
        var innerTable = CreateTable(
            widthPt: 120f,
            CreateRow(CreateCell()));

        var outerCell = CreateCell();
        outerCell.Children.Add(innerTable);

        var outerTable = CreateTable(
            widthPt: 300f,
            CreateRow(
                outerCell,
                CreateCell()));

        var result = Layout(outerTable, availableWidth: 500f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.DerivedColumnCount.ShouldBe(2);
        result.Rows[0].Cells.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(HtmlCssConstants.HtmlAttributes.Colspan, "Table cell colspan is not supported.")]
    [InlineData(HtmlCssConstants.HtmlAttributes.Rowspan, "Table cell rowspan is not supported.")]
    public void Layout_WhenCellHasUnsupportedSpanAttribute_ShouldReturnUnsupportedResultBeforeGeometryCalculation(
        string attributeName,
        string expectedReason)
    {
        var cell = CreateCell(element: CreateElement("TD", (attributeName, "2")));
        var table = CreateTable(
            widthPt: 120f,
            CreateRow(cell));

        var result = Layout(table, availableWidth: 200f);
        var diagnosticsSink = new Html2x.LayoutEngine.Geometry.Test.RecordingDiagnosticsSink();
        TableLayoutDiagnostics.EmitUnsupportedTable(
            nodePath: "html/body/table",
            structureKind: result.UnsupportedStructureKind ?? string.Empty,
            reason: result.UnsupportedReason ?? string.Empty,
            rowCount: result.RowCount,
            requestedWidth: result.RequestedWidth,
            resolvedWidth: result.ResolvedWidth,
            diagnosticsSink: diagnosticsSink);

        result.IsSupported.ShouldBeFalse();
        result.RowCount.ShouldBe(1);
        result.UnsupportedStructureKind.ShouldBe(attributeName);
        result.UnsupportedReason.ShouldBe(expectedReason);
        result.Rows.ShouldBeEmpty();
        result.ContentHeight.ShouldBe(0f);
        result.BorderBoxHeight.ShouldBe(0f);

        var unsupportedRecord = diagnosticsSink.Records.Single(e => e.Name == "layout/table");
        var reason = unsupportedRecord.Fields["reason"].ShouldBeOfType<DiagnosticStringValue>().Value;
        unsupportedRecord.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Unsupported"));
        unsupportedRecord.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(1));
        reason.ShouldContain(attributeName);
    }

    [Fact]
    public void Layout_SectionContainingNestedSection_ReturnsUnsupportedResult()
    {
        var innerSection = new TableSectionBox(BoxRole.TableSection);
        innerSection.Children.Add(CreateRow(CreateCell()));

        var outerSection = new TableSectionBox(BoxRole.TableSection);
        outerSection.Children.Add(innerSection);

        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 120f
            }
        };
        table.Children.Add(outerSection);

        var result = Layout(table, availableWidth: 200f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe("malformed-section-nesting");
        result.UnsupportedReason.ShouldBe("Table sections cannot contain nested table sections.");
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Layout_TallestCellOwnsRowHeightAndCellPlacements()
    {
        var paddedCell = CreateCell(new ComputedStyle
        {
            Padding = new Spacing(7.5f, 7.5f, 7.5f, 7.5f),
            Borders = BorderEdges.Uniform(new BorderSide(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
        });
        paddedCell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "A",
            Style = paddedCell.Style,
            Parent = paddedCell
        });

        var defaultCell = CreateCell();
        defaultCell.Children.Add(new InlineBox(BoxRole.Inline)
        {
            TextContent = "B",
            Style = defaultCell.Style,
            Parent = defaultCell
        });

        var table = CreateTable(
            widthPt: 120f,
            CreateRow(paddedCell, defaultCell));

        var result = Layout(table, availableWidth: 200f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);

        var row = result.Rows.ShouldHaveSingleItem();
        var firstPlacement = row.Cells[0];
        var secondPlacement = row.Cells[1];

        row.Y.ShouldBe(0f);
        row.Height.ShouldBe(30.9f, 0.2f);
        firstPlacement.Y.ShouldBe(0f);
        firstPlacement.Height.ShouldBe(row.Height, 0.01f);
        secondPlacement.Y.ShouldBe(0f);
        secondPlacement.Height.ShouldBe(row.Height, 0.01f);
        result.ContentHeight.ShouldBe(row.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.Height, 0.01f);
    }

    [Fact]
    public void Layout_CellWithStackedBlockChildren_UsesSharedCollapsedMarginHeight()
    {
        var cell = CreateCell();
        cell.Children.Add(new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                HeightPt = 10f,
                Margin = new Spacing(0f, 0f, 12f, 0f)
            }
        });
        cell.Children.Add(new BlockBox(BoxRole.Block)
        {
            Style = new ComputedStyle
            {
                HeightPt = 8f,
                Margin = new Spacing(4f, 0f, 0f, 0f)
            }
        });
        var table = CreateTable(widthPt: 120f, CreateRow(cell));

        var result = Layout(table, availableWidth: 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.Height.ShouldBe(30f, 0.01f);
        result.ContentHeight.ShouldBe(row.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.Height, 0.01f);
    }

    [Fact]
    public void Layout_CellWithNestedPaddedTable_UsesNestedTableBorderBoxHeight()
    {
        var innerTable = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = 80f,
                Padding = new Spacing(5f, 0f, 7f, 0f),
                Borders = BorderEdges.Uniform(new BorderSide(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        innerTable.Children.Add(CreateRow(CreateCell()));

        var outerCell = CreateCell();
        outerCell.Children.Add(innerTable);
        var outerTable = CreateTable(widthPt: 120f, CreateRow(outerCell));

        var result = Layout(outerTable, availableWidth: 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.Height.ShouldBe(36f, 0.01f);
        result.ContentHeight.ShouldBe(row.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.Height, 0.01f);
    }

    private static TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        return new TableLayoutEngine().Layout(table, availableWidth);
    }

    private static TableBox CreateTable(float? widthPt = null, params TableRowBox[] rows)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Style = new ComputedStyle
            {
                WidthPt = widthPt
            }
        };

        foreach (var row in rows)
        {
            table.Children.Add(row);
        }

        return table;
    }

    private static TableRowBox CreateRow(params TableCellBox[] cells)
    {
        var row = new TableRowBox(BoxRole.TableRow);

        foreach (var cell in cells)
        {
            row.Children.Add(cell);
        }

        return row;
    }

    private static TableCellBox CreateCell(ComputedStyle? style = null, StyledElementFacts? element = null)
    {
        return new TableCellBox(BoxRole.TableCell)
        {
            Style = style ?? new ComputedStyle(),
            Element = element
        };
    }

    private static StyledElementFacts CreateElement(string tagName, params (string Name, string Value)[] attributes)
    {
        return StyledElementFacts.Create(tagName, attributes);
    }
}
