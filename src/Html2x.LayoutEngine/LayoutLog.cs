using System;
using System.Collections.Generic;
using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Diagnostics.Contracts;
using Html2x.Abstractions.Measurements.Units;

namespace Html2x.LayoutEngine;

internal static class LayoutLog
{
    private const string Category = "layout/perf";

    public static void BuildStart(IDiagnosticSession? session, int htmlLength, PageSize pageSize)
    {
        Publish(
            session,
            "layout/build-start",
            payload =>
            {
                payload["htmlLength"] = htmlLength;
                payload["pageWidth"] = pageSize.Width;
                payload["pageHeight"] = pageSize.Height;
            });
    }

    public static void StageComplete(IDiagnosticSession? session, string stageName)
    {
        Publish(
            session,
            "layout/stage-complete",
            payload => payload["stage"] = stageName);
    }

    public static void BuildComplete(IDiagnosticSession? session, int blockCount)
    {
        Publish(
            session,
            "layout/build-complete",
            payload => payload["blockCount"] = blockCount);
    }

    private static void Publish(
        IDiagnosticSession? session,
        string kind,
        Action<Dictionary<string, object?>> configurePayload)
    {
        if (session is not { IsEnabled: true })
        {
            return;
        }

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        configurePayload(payload);

        var diagnosticEvent = new DiagnosticEvent(
            Guid.NewGuid(),
            session.Descriptor.SessionId,
            Category,
            kind,
            DateTimeOffset.UtcNow,
            payload);

        session.Publish(diagnosticEvent);
    }
}
