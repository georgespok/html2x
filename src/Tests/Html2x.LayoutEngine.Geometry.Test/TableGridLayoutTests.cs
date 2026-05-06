using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Box;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Geometry.Test;

public class TableGridLayoutTests
{
    [Fact]
    public void Layout_ExplicitWidthAndTwoCells_ResolvesRequestedTableWidth()
    {
        var table = CreateTable(
            400f,
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedWidth.ShouldBe(400f);
    }

    [Fact]
    public void Layout_WidestRowDefinesDerivedColumnCount()
    {
        var table = CreateTable(
            400f,
            CreateRow(
                CreateCell(),
                CreateCell()),
            CreateRow(
                CreateCell(),
                CreateCell(),
                CreateCell()));

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
    }

    [Fact]
    public void Layout_EqualWidthDistribution_SplitsResolvedWidthAcrossDerivedColumns()
    {
        var table = CreateTable(
            400f,
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.ColumnWidths.ShouldBe([200f, 200f]);
    }

    [Fact]
    public void Layout_TablePaddingAndBorder_SplitsContentWidthAcrossDerivedColumns()
    {
        var table = new TableBox(BoxRole.Table)
        {
            Style = new()
            {
                WidthPt = 120f,
                Padding = new(0f, 10f, 0f, 10f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        table.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeTrue();
        result.ResolvedWidth.ShouldBe(144f);
        result.ColumnWidths.ShouldBe([60f, 60f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(120f);
        result.Rows[0].Cells[1].UsedGeometry.X.ShouldBe(60f);
    }

    [Fact]
    public void Layout_TableSizing_UsesBlockMeasurementPolicy()
    {
        var table = new TableBox(BoxRole.Table)
        {
            MarkerOffset = 7f,
            Style = new()
            {
                Margin = new(0f, 20f, 0f, 10f),
                MinWidthPt = 120f,
                MaxWidthPt = 150f,
                Padding = new(0f, 5f, 0f, 5f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        table.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var result = Layout(table, 100f);

        result.ResolvedWidth.ShouldBe(134f);
        result.ColumnWidths.ShouldBe([56.5f, 56.5f]);
        result.Rows[0].UsedGeometry.BorderBoxRect.Width.ShouldBe(113f);
    }

    [Fact]
    public void Layout_TableExplicitWidth_ClampsToMaxWidth()
    {
        var table = CreateTable(
            220f,
            CreateRow(
                CreateCell(),
                CreateCell()));
        table.Style = table.Style with
        {
            MaxWidthPt = 150f
        };

        var result = Layout(table, 300f);

        result.ResolvedWidth.ShouldBe(150f);
        result.ColumnWidths.ShouldBe([75f, 75f]);
    }

    [Fact]
    public void Layout_RowPaddingAndBorder_PlacesCellsInsideRowContentBox()
    {
        var row = new TableRowBox(BoxRole.TableRow)
        {
            Style = new()
            {
                Padding = new(3f, 0f, 0f, 6f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        row.Children.Add(CreateCell());

        var table = new TableBox(BoxRole.Table)
        {
            Style = new()
            {
                WidthPt = 120f
            }
        };
        table.Children.Add(row);

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
        var cell = CreateCell(new()
        {
            Padding = new(3f, 4f, 5f, 6f),
            Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
        });
        var table = CreateTable(120f, CreateRow(cell));

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
        var cell = CreateCell();

        var placement = new TableLayoutCellPlacement(
            cell,
            0,
            false,
            UsedGeometryRules.FromBorderBox(
                new(0f, 0f, 10f, 10f),
                new(),
                new()));

        placement.UsedGeometry.BorderBoxRect.ShouldBe(new(0f, 0f, 10f, 10f));
    }

    [Fact]
    public void WriteSupported_TranslatesRowAndCellGeometryThroughGeometryOwner()
    {
        var row = new TableRowBox(BoxRole.TableRow);
        var cell = CreateCell();
        row.Children.Add(cell);
        var table = new TableBox(BoxRole.Table)
        {
            Style = new()
        };
        table.Children.Add(row);
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
            ResolvedWidth = 50f,
            DerivedColumnCount = 1,
            Rows =
            [
                new(
                    row,
                    0,
                    rowGeometry,
                    [new(cell, 0, false, cellGeometry)])
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
            static (_, _, _, _, _) => 0f);

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
    }

    [Fact]
    public void Layout_ShorterRows_DoNotChangeSharedColumnGrid()
    {
        var table = CreateTable(
            300f,
            CreateRow(
                CreateCell(),
                CreateCell(),
                CreateCell()),
            CreateRow(
                CreateCell(),
                CreateCell()));

        var result = Layout(table, 500f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(3);
        result.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public void Layout_SharedColumnGrid_DrivesEveryRowPlacement()
    {
        var table = CreateTable(
            300f,
            CreateRow(
                CreateCell(),
                CreateCell(),
                CreateCell()),
            CreateRow(
                CreateCell(),
                CreateCell()));

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
    [InlineData(HtmlCssConstants.HtmlAttributes.Colspan)]
    [InlineData(HtmlCssConstants.HtmlAttributes.Rowspan)]
    public void Layout_SpanOne_RemainsSupportedAndUsesCurrentGridFacts(string attributeName)
    {
        var cell = CreateCell(element: CreateElement("TD", (attributeName, "1")));
        var table = CreateTable(
            120f,
            CreateRow(cell, CreateCell()));

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeTrue();
        result.DerivedColumnCount.ShouldBe(2);
        result.ColumnWidths.ShouldBe([60f, 60f]);
        result.Rows.ShouldHaveSingleItem().Cells.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(HtmlCssConstants.HtmlAttributes.Colspan, "2")]
    [InlineData(HtmlCssConstants.HtmlAttributes.Rowspan, "2")]
    [InlineData(HtmlCssConstants.HtmlAttributes.Colspan, "invalid")]
    [InlineData(HtmlCssConstants.HtmlAttributes.Rowspan, "0")]
    public void Layout_UnsupportedSpans_StopBeforeGridFacts(string attributeName, string value)
    {
        var cell = CreateCell(element: CreateElement("TD", (attributeName, value)));
        var table = CreateTable(
            120f,
            CreateRow(cell));

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
        var headerCell = CreateCell(element: CreateElement("TH"));
        var bodyCell = CreateCell(element: CreateElement("TD"));
        var table = CreateTable(
            200f,
            CreateRow(headerCell),
            CreateRow(bodyCell));

        var result = Layout(table, 300f);

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
            Style = new()
            {
                WidthPt = 400f
            }
        };
        table.Children.Add(section);

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
            Style = new()
            {
                WidthPt = 400f
            }
        };
        table.Children.Add(section);

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
        var innerTable = CreateTable(
            120f,
            CreateRow(CreateCell()));

        var outerCell = CreateCell();
        outerCell.Children.Add(innerTable);

        var outerTable = CreateTable(
            300f,
            CreateRow(
                outerCell,
                CreateCell()));

        var result = Layout(outerTable, 500f);

        result.IsSupported.ShouldBeTrue();
        result.Rows.Count.ShouldBe(1);
        result.DerivedColumnCount.ShouldBe(2);
        result.Rows[0].Cells.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(HtmlCssConstants.HtmlAttributes.Colspan, "Table cell colspan is not supported.")]
    [InlineData(HtmlCssConstants.HtmlAttributes.Rowspan, "Table cell rowspan is not supported.")]
    public void Layout_UnsupportedCellSpan_ReturnsUnsupportedBeforeGeometry(
        string attributeName,
        string expectedReason)
    {
        var cell = CreateCell(element: CreateElement("TD", (attributeName, "2")));
        var table = CreateTable(
            120f,
            CreateRow(cell));

        var result = Layout(table, 200f);
        var diagnosticsSink = new RecordingDiagnosticsSink();
        TableGridDiagnostics.EmitUnsupportedTable(
            "html/body/table",
            result.UnsupportedStructureKind ?? string.Empty,
            result.UnsupportedReason ?? string.Empty,
            result.RowCount,
            result.RequestedWidth,
            result.ResolvedWidth,
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
            Style = new()
            {
                WidthPt = 120f
            }
        };
        table.Children.Add(outerSection);

        var result = Layout(table, 200f);

        result.IsSupported.ShouldBeFalse();
        result.UnsupportedStructureKind.ShouldBe("malformed-section-nesting");
        result.UnsupportedReason.ShouldBe("Table sections cannot contain nested table sections.");
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Layout_TallestCellOwnsRowHeightAndCellPlacements()
    {
        var paddedCell = CreateCell(new()
        {
            Padding = new(7.5f, 7.5f, 7.5f, 7.5f),
            Borders = BorderEdges.Uniform(new(0.75f, ColorRgba.Black, BorderLineStyle.Solid))
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
            120f,
            CreateRow(paddedCell, defaultCell));

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
        var cell = CreateCell();
        cell.Children.Add(new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                HeightPt = 10f,
                Margin = new(0f, 0f, 12f, 0f)
            }
        });
        cell.Children.Add(new BlockBox(BoxRole.Block)
        {
            Style = new()
            {
                HeightPt = 8f,
                Margin = new(4f, 0f, 0f, 0f)
            }
        });
        var table = CreateTable(120f, CreateRow(cell));

        var result = Layout(table, 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.UsedGeometry.Height.ShouldBe(30f, 0.01f);
        result.ContentHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
    }

    [Fact]
    public void Layout_CellWithNestedPaddedTable_UsesNestedTableBorderBoxHeight()
    {
        var innerTable = new TableBox(BoxRole.Table)
        {
            Style = new()
            {
                WidthPt = 80f,
                Padding = new(5f, 0f, 7f, 0f),
                Borders = BorderEdges.Uniform(new(2f, ColorRgba.Black, BorderLineStyle.Solid))
            }
        };
        innerTable.Children.Add(CreateRow(CreateCell()));

        var outerCell = CreateCell();
        outerCell.Children.Add(innerTable);
        var outerTable = CreateTable(120f, CreateRow(outerCell));

        var result = Layout(outerTable, 200f);

        var row = result.Rows.ShouldHaveSingleItem();
        row.UsedGeometry.Height.ShouldBe(36f, 0.01f);
        result.ContentHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
        result.BorderBoxHeight.ShouldBe(row.UsedGeometry.Height, 0.01f);
    }

    private static TableLayoutResult Layout(TableBox table, float availableWidth) =>
        new TableGridLayout().Layout(table, availableWidth);

    private static TableBox CreateTable(float? widthPt = null, params TableRowBox[] rows)
    {
        var table = new TableBox(BoxRole.Table)
        {
            Style = new()
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

    private static TableCellBox CreateCell(ComputedStyle? style = null, StyledElementFacts? element = null) =>
        new(BoxRole.TableCell)
        {
            Style = style ?? new ComputedStyle(),
            Element = element
        };

    private static StyledElementFacts CreateElement(string tagName, params (string Name, string Value)[] attributes) =>
        StyledElementFacts.Create(tagName, attributes);
}