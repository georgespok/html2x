using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel.Text;

namespace Html2x.Text;

public sealed class DiagnosticsFontSource(
    IFontSource inner,
    IDiagnosticsSink diagnosticsSink) : IFontSource
{
    private readonly IDiagnosticsSink _diagnosticsSink =
        diagnosticsSink ?? throw new ArgumentNullException(nameof(diagnosticsSink));

    private readonly object _gate = new();
    private readonly IFontSource _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly HashSet<ResolutionEventKey> _published = [];

    public ResolvedFont Resolve(FontKey requested, string consumer)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);

        try
        {
            var resolved = _inner.Resolve(requested, consumer);
            PublishOnce(
                requested,
                consumer,
                DiagnosticSeverity.Info,
                FontDiagnosticNames.Outcomes.Resolved,
                null,
                resolved);
            return resolved;
        }
        catch (Exception exception)
        {
            PublishOnce(
                requested,
                consumer,
                DiagnosticSeverity.Error,
                FontDiagnosticNames.Outcomes.Failed,
                exception.Message,
                CreateFailedResolution(exception));
            throw;
        }
    }

    private void PublishOnce(
        FontKey requested,
        string consumer,
        DiagnosticSeverity severity,
        string outcome,
        string? reason,
        ResolvedFont? resolved)
    {
        var key = new ResolutionEventKey(
            consumer,
            requested.Family,
            requested.Weight,
            requested.Style,
            resolved?.SourceId,
            outcome);

        lock (_gate)
        {
            if (!_published.Add(key))
            {
                return;
            }
        }

        _diagnosticsSink.Emit(new(
            FontDiagnosticNames.Stages.Font,
            FontDiagnosticNames.Events.Resolve,
            severity,
            reason,
            null,
            DiagnosticFields.Create(
                DiagnosticFields.Field(FontDiagnosticNames.Fields.Owner, nameof(FontPathSource)),
                DiagnosticFields.Field(FontDiagnosticNames.Fields.Consumer, consumer),
                DiagnosticFields.Field(FontDiagnosticNames.Fields.RequestedFamily, requested.Family),
                DiagnosticFields.Field(FontDiagnosticNames.Fields.RequestedWeight,
                    DiagnosticValue.FromEnum(requested.Weight)),
                DiagnosticFields.Field(FontDiagnosticNames.Fields.RequestedStyle,
                    DiagnosticValue.FromEnum(requested.Style)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.ResolvedFamily,
                    resolved?.Family is null ? null : DiagnosticValue.From(resolved.Family)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.ResolvedWeight,
                    resolved is null ? null : DiagnosticValue.FromEnum(resolved.Weight)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.ResolvedStyle,
                    resolved is null ? null : DiagnosticValue.FromEnum(resolved.Style)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.SourceId,
                    resolved?.SourceId is null ? null : DiagnosticValue.From(resolved.SourceId)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.ConfiguredPath,
                    resolved?.ConfiguredPath is null ? null : DiagnosticValue.From(resolved.ConfiguredPath)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.FilePath,
                    resolved?.FilePath is null ? null : DiagnosticValue.From(resolved.FilePath)),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.FaceIndex,
                    resolved is null ? null : DiagnosticValue.From(resolved.FaceIndex)),
                DiagnosticFields.Field(FontDiagnosticNames.Fields.Outcome, outcome),
                DiagnosticFields.Field(
                    FontDiagnosticNames.Fields.Reason,
                    reason is null ? null : DiagnosticValue.From(reason))),
            DateTimeOffset.UtcNow));
    }

    private static ResolvedFont? CreateFailedResolution(Exception exception)
    {
        if (exception is not FontResolutionException fontException)
        {
            return null;
        }

        var resolved = fontException.ResolvedFont;
        var configuredPath = fontException.ConfiguredPath;
        var filePath = resolved?.FilePath;
        var sourceId = resolved?.SourceId
                       ?? fontException.ResolvedPath
                       ?? configuredPath;

        if (string.IsNullOrWhiteSpace(sourceId) &&
            string.IsNullOrWhiteSpace(configuredPath) &&
            string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        return new(
            resolved?.Family ?? string.Empty,
            resolved?.Weight ?? default,
            resolved?.Style ?? default,
            sourceId ?? string.Empty,
            filePath,
            resolved?.FaceIndex ?? 0,
            configuredPath);
    }

    private readonly record struct ResolutionEventKey(
        string Consumer,
        string RequestedFamily,
        FontWeight RequestedWeight,
        FontStyle RequestedStyle,
        string? SourceId,
        string Outcome);
}