using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Geometry.Diagnostics;
using Html2x.RenderModel.Fragments;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public class TableLayoutDiagnosticsTests
{
    [Fact]
    public void EmitSupportedTable_AddsRecordWithTableFields()
    {
        var sink = new RecordingDiagnosticsSink();

        TableLayoutDiagnostics.EmitSupportedTable(
            nodePath: "html/body/table",
            rowCount: 2,
            derivedColumnCount: 2,
            requestedWidth: 400f,
            resolvedWidth: 400f,
            rowContexts:
            [
                new TableRowDiagnosticContext(0, 2, 24f),
                new TableRowDiagnosticContext(1, 2, 24f)
            ],
            cellContexts:
            [
                new TableCellDiagnosticContext(0, 0, true, 200f, 24f),
                new TableCellDiagnosticContext(0, 1, true, 200f, 24f),
                new TableCellDiagnosticContext(1, 0, false, 200f, 24f),
                new TableCellDiagnosticContext(1, 1, false, 200f, 24f)
            ],
            columnContexts:
            [
                new TableColumnDiagnosticContext(0, 200f),
                new TableColumnDiagnosticContext(1, 200f)
            ],
            groupContexts:
            [
                new TableGroupDiagnosticContext("thead", 1),
                new TableGroupDiagnosticContext("tbody", 1)
            ],
            diagnosticsSink: sink);

        var record = sink.Records.ShouldHaveSingleItem();
        record.Severity.ShouldBe(DiagnosticSeverity.Info);
        record.Name.ShouldBe("layout/table");
        record.Fields["nodePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        record.Fields["tablePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        record.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(2));
        record.Fields["derivedColumnCount"].ShouldBe(new DiagnosticNumberValue(2));
        record.Fields["requestedWidth"].ShouldBe(new DiagnosticNumberValue(400f));
        record.Fields["resolvedWidth"].ShouldBe(new DiagnosticNumberValue(400f));
        record.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Supported"));
        record.Fields["reason"].ShouldBeNull();

        var rows = record.Fields["rows"].ShouldBeOfType<DiagnosticArray>();
        rows.Count.ShouldBe(2);
        rows[0].ShouldBeOfType<DiagnosticObject>()["rowIndex"].ShouldBe(new DiagnosticNumberValue(0));
        rows[0].ShouldBeOfType<DiagnosticObject>()["cellCount"].ShouldBe(new DiagnosticNumberValue(2));

        var cells = record.Fields["cells"].ShouldBeOfType<DiagnosticArray>();
        cells.Count.ShouldBe(4);
        cells[0].ShouldBeOfType<DiagnosticObject>()["rowIndex"].ShouldBe(new DiagnosticNumberValue(0));
        cells[0].ShouldBeOfType<DiagnosticObject>()["columnIndex"].ShouldBe(new DiagnosticNumberValue(0));
        cells[0].ShouldBeOfType<DiagnosticObject>()["isHeader"].ShouldBe(new DiagnosticBooleanValue(true));

        var columns = record.Fields["columns"].ShouldBeOfType<DiagnosticArray>();
        columns.Select(static column => column.ShouldBeOfType<DiagnosticObject>()["width"])
            .ShouldBe([new DiagnosticNumberValue(200f), new DiagnosticNumberValue(200f)]);
        var groups = record.Fields["groups"].ShouldBeOfType<DiagnosticArray>();
        groups.Select(static group => group.ShouldBeOfType<DiagnosticObject>()["groupKind"])
            .ShouldBe([new DiagnosticStringValue("thead"), new DiagnosticStringValue("tbody")]);
    }

    [Fact]
    public void EmitSupportedTable_DiagnosticsSink_FlattensTableFields()
    {
        var sink = new RecordingDiagnosticsSink();

        TableLayoutDiagnostics.EmitSupportedTable(
            nodePath: "html/body/table",
            rowCount: 2,
            derivedColumnCount: 2,
            requestedWidth: 400f,
            resolvedWidth: 380f,
            rowContexts:
            [
                new TableRowDiagnosticContext(0, 2, 24f),
                new TableRowDiagnosticContext(1, 2, 26f)
            ],
            columnContexts:
            [
                new TableColumnDiagnosticContext(0, 180f),
                new TableColumnDiagnosticContext(1, 200f)
            ],
            diagnosticsSink: sink);

        var record = sink.Records.ShouldHaveSingleItem();
        record.Stage.ShouldBe("stage/box-tree");
        record.Name.ShouldBe("layout/table");
        record.Fields["nodePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        record.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(2));
        record.Fields["derivedColumnCount"].ShouldBe(new DiagnosticNumberValue(2));
        record.Fields["requestedWidth"].ShouldBe(new DiagnosticNumberValue(400f));
        record.Fields["resolvedWidth"].ShouldBe(new DiagnosticNumberValue(380f));
        record.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Supported"));

        var columns = record.Fields["columns"].ShouldBeOfType<DiagnosticArray>();
        columns.Count.ShouldBe(2);
        var firstColumn = columns[0].ShouldBeOfType<DiagnosticObject>();
        firstColumn["columnIndex"].ShouldBe(new DiagnosticNumberValue(0));
        firstColumn["width"].ShouldBe(new DiagnosticNumberValue(180f));
    }

    [Fact]
    public void EmitUnsupportedTable_AddsUnsupportedStructureAndTableRecords()
    {
        var sink = new RecordingDiagnosticsSink();

        TableLayoutDiagnostics.EmitUnsupportedTable(
            nodePath: "html/body/table",
            structureKind: "colspan",
            reason: "colspan is not supported",
            rowCount: 1,
            requestedWidth: 400f,
            resolvedWidth: 400f,
            diagnosticsSink: sink);

        sink.Records.Count.ShouldBe(2);
        var unsupported = sink.Records[0];
        unsupported.Severity.ShouldBe(DiagnosticSeverity.Error);
        unsupported.Name.ShouldBe("layout/table/unsupported-structure");
        unsupported.Fields["nodePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        unsupported.Fields["structureKind"].ShouldBe(new DiagnosticStringValue("colspan"));
        unsupported.Fields["reason"].ShouldBe(new DiagnosticStringValue("colspan is not supported"));
        unsupported.Fields["formattingContext"].ShouldBe(new DiagnosticStringValue(nameof(FormattingContextKind.Block)));

        var table = sink.Records[1];
        table.Severity.ShouldBe(DiagnosticSeverity.Info);
        table.Name.ShouldBe("layout/table");
        table.Fields["nodePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        table.Fields["tablePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        table.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(1));
        table.Fields["derivedColumnCount"].ShouldBeNull();
        table.Fields["requestedWidth"].ShouldBe(new DiagnosticNumberValue(400f));
        table.Fields["resolvedWidth"].ShouldBe(new DiagnosticNumberValue(400f));
        table.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Unsupported"));
        table.Fields["reason"].ShouldBe(new DiagnosticStringValue("colspan is not supported"));
    }

    [Fact]
    public void EmitUnsupportedTable_DiagnosticsSink_FlattensOutcome()
    {
        var sink = new RecordingDiagnosticsSink();

        TableLayoutDiagnostics.EmitUnsupportedTable(
            nodePath: "html/body/table",
            structureKind: "colspan",
            reason: "colspan is not supported",
            rowCount: 1,
            requestedWidth: 400f,
            resolvedWidth: 400f,
            diagnosticsSink: sink);

        sink.Records.Count.ShouldBe(2);
        var unsupported = sink.Records[0];
        unsupported.Name.ShouldBe("layout/table/unsupported-structure");
        unsupported.Severity.ShouldBe(DiagnosticSeverity.Error);
        unsupported.Fields["nodePath"].ShouldBe(new DiagnosticStringValue("html/body/table"));
        unsupported.Fields["structureKind"].ShouldBe(new DiagnosticStringValue("colspan"));
        unsupported.Fields["reason"].ShouldBe(new DiagnosticStringValue("colspan is not supported"));

        var table = sink.Records[1];
        table.Name.ShouldBe("layout/table");
        table.Fields["rowCount"].ShouldBe(new DiagnosticNumberValue(1));
        table.Fields["derivedColumnCount"].ShouldBeNull();
        table.Fields["outcome"].ShouldBe(new DiagnosticStringValue("Unsupported"));
        table.Fields["reason"].ShouldBe(new DiagnosticStringValue("colspan is not supported"));
    }
}
