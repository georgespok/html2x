using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Diagnostics;

public static class TableLayoutDiagnostics
{
    private const string SupportedTableEvent = "layout/table";
    private const string UnsupportedTableEvent = "layout/table/unsupported-structure";

    public static void EmitSupportedTable(
        DiagnosticsSession? diagnosticsSession,
        string nodePath,
        int rowCount,
        int derivedColumnCount,
        float? requestedWidth,
        float? resolvedWidth)
    {
        Emit(
            diagnosticsSession,
            DiagnosticsEventType.Trace,
            SupportedTableEvent,
            new TableLayoutPayload
            {
                NodePath = nodePath,
                RowCount = rowCount,
                DerivedColumnCount = derivedColumnCount,
                RequestedWidth = requestedWidth,
                ResolvedWidth = resolvedWidth,
                Outcome = "Supported"
            });
    }

    public static void EmitUnsupportedTable(
        DiagnosticsSession? diagnosticsSession,
        string nodePath,
        string structureKind,
        string reason,
        int rowCount = 0,
        float? requestedWidth = null,
        float? resolvedWidth = null,
        FormattingContextKind formattingContext = FormattingContextKind.Block)
    {
        Emit(
            diagnosticsSession,
            DiagnosticsEventType.Error,
            UnsupportedTableEvent,
            new UnsupportedStructurePayload
            {
                NodePath = nodePath,
                StructureKind = structureKind,
                Reason = reason,
                FormattingContext = formattingContext
            });

        Emit(
            diagnosticsSession,
            DiagnosticsEventType.Trace,
            SupportedTableEvent,
            new TableLayoutPayload
            {
                NodePath = nodePath,
                RowCount = rowCount,
                DerivedColumnCount = null,
                RequestedWidth = requestedWidth,
                ResolvedWidth = resolvedWidth,
                Outcome = "Unsupported",
                Reason = reason
            });
    }

    private static void Emit(
        DiagnosticsSession? diagnosticsSession,
        DiagnosticsEventType eventType,
        string eventName,
        IDiagnosticsPayload payload)
    {
        diagnosticsSession?.Events.Add(new DiagnosticsEvent
        {
            Type = eventType,
            Name = eventName,
            Payload = payload
        });
    }
}
