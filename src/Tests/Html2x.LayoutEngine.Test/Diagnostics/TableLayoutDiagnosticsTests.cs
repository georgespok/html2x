using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Diagnostics;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public class TableLayoutDiagnosticsTests
{
    [Fact]
    public void EmitSupportedTable_AddsTraceEventWithTableLayoutPayload()
    {
        var diagnosticsSession = new DiagnosticsSession();

        TableLayoutDiagnostics.EmitSupportedTable(
            diagnosticsSession,
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
            ]);

        diagnosticsSession.Events.ShouldHaveSingleItem();
        diagnosticsSession.Events[0].Type.ShouldBe(DiagnosticsEventType.Trace);
        diagnosticsSession.Events[0].Name.ShouldBe("layout/table");
        diagnosticsSession.Events[0].Payload.ShouldBeOfType<TableLayoutPayload>();

        var payload = (TableLayoutPayload)diagnosticsSession.Events[0].Payload!;
        payload.NodePath.ShouldBe("html/body/table");
        payload.TablePath.ShouldBe("html/body/table");
        payload.RowCount.ShouldBe(2);
        payload.DerivedColumnCount.ShouldBe(2);
        payload.RequestedWidth.ShouldBe(400f);
        payload.ResolvedWidth.ShouldBe(400f);
        payload.Outcome.ShouldBe("Supported");
        payload.Reason.ShouldBeNull();
        payload.RowContexts.Count.ShouldBe(2);
        payload.RowContexts[0].RowIndex.ShouldBe(0);
        payload.RowContexts[0].CellCount.ShouldBe(2);
        payload.CellContexts.Count.ShouldBe(4);
        payload.CellContexts[0].RowIndex.ShouldBe(0);
        payload.CellContexts[0].ColumnIndex.ShouldBe(0);
        payload.CellContexts[0].IsHeader.ShouldBeTrue();
        payload.ColumnContexts.Select(static column => column.Width).ShouldBe([200f, 200f]);
        payload.GroupContexts.Select(static group => group.GroupKind).ShouldBe(["thead", "tbody"]);
    }

    [Fact]
    public void EmitUnsupportedTable_AddsErrorEventWithUnsupportedStructurePayload()
    {
        var diagnosticsSession = new DiagnosticsSession();

        TableLayoutDiagnostics.EmitUnsupportedTable(
            diagnosticsSession,
            nodePath: "html/body/table",
            structureKind: "colspan",
            reason: "colspan is not supported",
            rowCount: 1,
            requestedWidth: 400f,
            resolvedWidth: 400f);

        diagnosticsSession.Events.Count.ShouldBe(2);
        diagnosticsSession.Events[0].Type.ShouldBe(DiagnosticsEventType.Error);
        diagnosticsSession.Events[0].Name.ShouldBe("layout/table/unsupported-structure");
        diagnosticsSession.Events[0].Payload.ShouldBeOfType<UnsupportedStructurePayload>();

        var payload = (UnsupportedStructurePayload)diagnosticsSession.Events[0].Payload!;
        payload.NodePath.ShouldBe("html/body/table");
        payload.StructureKind.ShouldBe("colspan");
        payload.Reason.ShouldBe("colspan is not supported");
        payload.FormattingContext.ShouldBe(FormattingContextKind.Block);

        diagnosticsSession.Events[1].Type.ShouldBe(DiagnosticsEventType.Trace);
        diagnosticsSession.Events[1].Name.ShouldBe("layout/table");
        diagnosticsSession.Events[1].Payload.ShouldBeOfType<TableLayoutPayload>();

        var layoutPayload = (TableLayoutPayload)diagnosticsSession.Events[1].Payload!;
        layoutPayload.NodePath.ShouldBe("html/body/table");
        layoutPayload.TablePath.ShouldBe("html/body/table");
        layoutPayload.RowCount.ShouldBe(1);
        layoutPayload.DerivedColumnCount.ShouldBeNull();
        layoutPayload.RequestedWidth.ShouldBe(400f);
        layoutPayload.ResolvedWidth.ShouldBe(400f);
        layoutPayload.Outcome.ShouldBe("Unsupported");
        layoutPayload.Reason.ShouldBe("colspan is not supported");
    }
}
