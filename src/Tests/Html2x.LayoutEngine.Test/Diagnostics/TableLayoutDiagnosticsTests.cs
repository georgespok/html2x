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
            resolvedWidth: 400f);

        diagnosticsSession.Events.ShouldHaveSingleItem();
        diagnosticsSession.Events[0].Type.ShouldBe(DiagnosticsEventType.Trace);
        diagnosticsSession.Events[0].Name.ShouldBe("layout/table");
        diagnosticsSession.Events[0].Payload.ShouldBeOfType<TableLayoutPayload>();

        var payload = (TableLayoutPayload)diagnosticsSession.Events[0].Payload!;
        payload.NodePath.ShouldBe("html/body/table");
        payload.RowCount.ShouldBe(2);
        payload.DerivedColumnCount.ShouldBe(2);
        payload.RequestedWidth.ShouldBe(400f);
        payload.ResolvedWidth.ShouldBe(400f);
        payload.Outcome.ShouldBe("Supported");
        payload.Reason.ShouldBeNull();
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
        layoutPayload.RowCount.ShouldBe(1);
        layoutPayload.DerivedColumnCount.ShouldBeNull();
        layoutPayload.RequestedWidth.ShouldBe(400f);
        layoutPayload.ResolvedWidth.ShouldBe(400f);
        layoutPayload.Outcome.ShouldBe("Unsupported");
        layoutPayload.Reason.ShouldBe("colspan is not supported");
    }
}
