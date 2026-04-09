using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Styles;
using Html2x.LayoutEngine;
using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using Moq;
using Shouldly;

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
        var section = new InlineBox(DisplayRole.Inline);
        section.Children.Add(CreateRow(
            CreateCell(),
            CreateCell()));

        var table = new TableBox(DisplayRole.Table)
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
        var section = new TableSectionBox(DisplayRole.TableSection);
        section.Children.Add(firstRow);
        section.Children.Add(secondRow);

        var table = new TableBox(DisplayRole.Table)
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

    [Fact]
    public void Layout_CellWithColspan_ReturnsUnsupportedResultBeforeGeometryCalculation()
    {
        var cell = CreateCell(element: CreateElement("TD", (HtmlCssConstants.HtmlAttributes.Colspan, "2")));
        var table = CreateTable(
            widthPt: 120f,
            CreateRow(cell));

        var result = Layout(table, availableWidth: 200f);
        var diagnosticsSession = new DiagnosticsSession();
        TableLayoutDiagnostics.EmitUnsupportedTable(
            diagnosticsSession,
            nodePath: "html/body/table",
            structureKind: result.UnsupportedStructureKind ?? string.Empty,
            reason: result.UnsupportedReason ?? string.Empty,
            rowCount: result.RowCount,
            requestedWidth: result.RequestedWidth,
            resolvedWidth: result.ResolvedWidth);

        result.IsSupported.ShouldBeFalse();
        result.RowCount.ShouldBe(1);
        result.UnsupportedStructureKind.ShouldBe(HtmlCssConstants.HtmlAttributes.Colspan);
        result.UnsupportedReason.ShouldBe("Table cell colspan is not supported.");
        result.Rows.ShouldBeEmpty();
        result.Height.ShouldBe(0f);

        var unsupportedPayload = diagnosticsSession.Events
            .Single(e => e.Name == "layout/table")
            .Payload.ShouldBeOfType<TableLayoutPayload>();
        var reason = unsupportedPayload.Reason!;
        unsupportedPayload.Outcome.ShouldBe("Unsupported");
        unsupportedPayload.RowCount.ShouldBe(1);
        reason.ShouldContain("colspan");
    }

    [Fact]
    public void Layout_CellWithRowspan_ReturnsUnsupportedResultBeforeGeometryCalculation()
    {
        var cell = CreateCell(element: CreateElement("TD", (HtmlCssConstants.HtmlAttributes.Rowspan, "2")));
        var table = CreateTable(
            widthPt: 120f,
            CreateRow(cell));

        var result = Layout(table, availableWidth: 200f);
        var diagnosticsSession = new DiagnosticsSession();
        TableLayoutDiagnostics.EmitUnsupportedTable(
            diagnosticsSession,
            nodePath: "html/body/table",
            structureKind: result.UnsupportedStructureKind ?? string.Empty,
            reason: result.UnsupportedReason ?? string.Empty,
            rowCount: result.RowCount,
            requestedWidth: result.RequestedWidth,
            resolvedWidth: result.ResolvedWidth);

        result.IsSupported.ShouldBeFalse();
        result.RowCount.ShouldBe(1);
        result.UnsupportedStructureKind.ShouldBe(HtmlCssConstants.HtmlAttributes.Rowspan);
        result.UnsupportedReason.ShouldBe("Table cell rowspan is not supported.");
        result.Rows.ShouldBeEmpty();
        result.Height.ShouldBe(0f);

        var unsupportedPayload = diagnosticsSession.Events
            .Single(e => e.Name == "layout/table")
            .Payload.ShouldBeOfType<TableLayoutPayload>();
        var reason = unsupportedPayload.Reason!;
        unsupportedPayload.Outcome.ShouldBe("Unsupported");
        unsupportedPayload.RowCount.ShouldBe(1);
        reason.ShouldContain("rowspan");
    }

    [Fact]
    public void Layout_SectionContainingNestedSection_ReturnsUnsupportedResult()
    {
        var innerSection = new TableSectionBox(DisplayRole.TableSection);
        innerSection.Children.Add(CreateRow(CreateCell()));

        var outerSection = new TableSectionBox(DisplayRole.TableSection);
        outerSection.Children.Add(innerSection);

        var table = new TableBox(DisplayRole.Table)
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
        paddedCell.Children.Add(new InlineBox(DisplayRole.Inline)
        {
            TextContent = "A",
            Style = paddedCell.Style,
            Parent = paddedCell
        });

        var defaultCell = CreateCell();
        defaultCell.Children.Add(new InlineBox(DisplayRole.Inline)
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
        result.Height.ShouldBe(row.Height, 0.01f);
    }

    private static TableLayoutResult Layout(TableBox table, float availableWidth)
    {
        return new TableLayoutEngine().Layout(table, availableWidth);
    }

    private static TableBox CreateTable(float? widthPt = null, params TableRowBox[] rows)
    {
        var table = new TableBox(DisplayRole.Table)
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
        var row = new TableRowBox(DisplayRole.TableRow);

        foreach (var cell in cells)
        {
            row.Children.Add(cell);
        }

        return row;
    }

    private static TableCellBox CreateCell(ComputedStyle? style = null, AngleSharp.Dom.IElement? element = null)
    {
        return new TableCellBox(DisplayRole.TableCell)
        {
            Style = style ?? new ComputedStyle(),
            Element = element
        };
    }

    private static AngleSharp.Dom.IElement CreateElement(string tagName, params (string Name, string Value)[] attributes)
    {
        var element = new Mock<AngleSharp.Dom.IElement>();
        element.SetupGet(x => x.TagName).Returns(tagName);

        foreach (var attribute in attributes)
        {
            element.Setup(x => x.HasAttribute(attribute.Name)).Returns(true);
            element.Setup(x => x.GetAttribute(attribute.Name)).Returns(attribute.Value);
        }

        return element.Object;
    }
}
