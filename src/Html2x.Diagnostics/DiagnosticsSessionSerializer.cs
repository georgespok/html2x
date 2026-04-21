using System.Text.Encodings.Web;
using System.Text.Json;
using Html2x.Abstractions.Diagnostics;

namespace Html2x.Diagnostics;

public static class DiagnosticsSessionSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(DiagnosticsSession session)
    {
        return JsonSerializer.Serialize(ToSerializableObject(session), JsonOptions);
    }

    public static object ToSerializableObject(DiagnosticsSession session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var envelope = new DiagnosticsSessionEnvelope
        {
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Options = session.Options,
            Events = session.Events.Select(MapEvent).ToArray()
        };

        return envelope;
    }

    private static DiagnosticsEventEnvelope MapEvent(DiagnosticsEvent @event)
    {
        return new DiagnosticsEventEnvelope
        {
            Type = @event.Type.ToString(),
            Name = @event.Name,
            Description = @event.Description,
            Timestamp = @event.Timestamp,
            Severity = @event.Severity?.ToString(),
            StageState = @event.StageState?.ToString(),
            Context = MapContext(@event.Context),
            RawUserInput = @event.RawUserInput,
            Payload = MapPayload(@event.Payload)
        };
    }

    private static DiagnosticsContextEnvelope? MapContext(DiagnosticContext? context)
    {
        return context is null
            ? null
            : new DiagnosticsContextEnvelope
            {
                Selector = context.Selector,
                ElementIdentity = context.ElementIdentity,
                StyleDeclaration = context.StyleDeclaration,
                StructuralPath = context.StructuralPath,
                RawUserInput = context.RawUserInput
            };
    }

    private static object? MapPayload(IDiagnosticsPayload? payload)
    {
        return payload switch
        {
            HtmlPayload html => new
            {
                html.Kind,
                html.Html,
                html.ImageStatus,
                html.AppliedImageScale,
                html.ImageWarning
            },
            LayoutSnapshotPayload layout => new
            {
                layout.Kind,
                layout.Snapshot
            },
            GeometrySnapshotPayload geometry => new
            {
                geometry.Kind,
                geometry.Snapshot
            },
            RenderSummaryPayload summary => new
            {
                summary.Kind,
                summary.PageCount,
                summary.PdfSize
            },
            MarginCollapsePayload margin => new
            {
                margin.Kind,
                margin.PreviousBottomMargin,
                margin.NextTopMargin,
                margin.CollapsedTopMargin
            },
            TableLayoutPayload table => MapTablePayload(table),
            UnsupportedStructurePayload unsupported => new
            {
                unsupported.Kind,
                unsupported.NodePath,
                unsupported.StructureKind,
                unsupported.Reason,
                unsupported.FormattingContext
            },
            PaginationTracePayload pagination => MapPaginationPayload(pagination),
            StyleDiagnosticPayload style => new
            {
                style.Kind,
                style.PropertyName,
                style.RawValue,
                style.NormalizedValue,
                style.Decision,
                style.Reason,
                Context = MapContext(style.Context)
            },
            ImageRenderPayload image => MapImagePayload(image),
            null => null,
            _ => new { payload.Kind }
        };
    }

    private static object MapTablePayload(TableLayoutPayload table)
    {
        return new
        {
            table.Kind,
            table.NodePath,
            table.TablePath,
            table.RowCount,
            table.DerivedColumnCount,
            table.RequestedWidth,
            table.ResolvedWidth,
            table.Outcome,
            table.Reason,
            table.RowContexts,
            table.CellContexts,
            table.ColumnContexts,
            table.GroupContexts
        };
    }

    private static object MapPaginationPayload(PaginationTracePayload pagination)
    {
        return new
        {
            pagination.Kind,
            pagination.EventName,
            Severity = pagination.Severity.ToString(),
            Context = MapContext(pagination.Context),
            pagination.PageNumber,
            pagination.FragmentId,
            pagination.FromPage,
            pagination.ToPage,
            pagination.LocalY,
            pagination.RemainingSpace,
            pagination.RemainingSpaceBefore,
            pagination.RemainingSpaceAfter,
            pagination.BlockHeight,
            pagination.PageContentHeight,
            pagination.Reason
        };
    }

    private static object MapImagePayload(ImageRenderPayload image)
    {
        return new
        {
            image.Kind,
            image.Src,
            Severity = image.Severity.ToString(),
            Context = MapContext(image.Context),
            RenderedWidth = image.RenderedSize.Width,
            RenderedHeight = image.RenderedSize.Height,
            Status = image.Status.ToString(),
            image.Borders
        };
    }

    private sealed class DiagnosticsSessionEnvelope
    {
        public DateTimeOffset StartTime { get; init; }

        public DateTimeOffset EndTime { get; init; }

        public object Options { get; init; } = null!;

        public IReadOnlyList<DiagnosticsEventEnvelope> Events { get; init; } = Array.Empty<DiagnosticsEventEnvelope>();
    }

    private sealed class DiagnosticsEventEnvelope
    {
        public string Type { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DateTimeOffset Timestamp { get; init; }

        public string? Severity { get; init; }

        public string? StageState { get; init; }

        public DiagnosticsContextEnvelope? Context { get; init; }

        public string? RawUserInput { get; init; }

        public object? Payload { get; init; }
    }

    private sealed class DiagnosticsContextEnvelope
    {
        public string? Selector { get; init; }

        public string? ElementIdentity { get; init; }

        public string? StyleDeclaration { get; init; }

        public string? StructuralPath { get; init; }

        public string? RawUserInput { get; init; }
    }
}
