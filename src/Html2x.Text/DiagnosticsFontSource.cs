using Html2x.Diagnostics.Contracts;
using Html2x.RenderModel;

namespace Html2x.Text;

public sealed class DiagnosticsFontSource : IFontSource
{
    private readonly IFontSource _inner;
    private readonly IDiagnosticsSink _diagnosticsSink;
    private readonly HashSet<ResolutionEventKey> _published = [];
    private readonly object _gate = new();

    public DiagnosticsFontSource(
        IFontSource inner,
        IDiagnosticsSink diagnosticsSink)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _diagnosticsSink = diagnosticsSink ?? throw new ArgumentNullException(nameof(diagnosticsSink));
    }

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
                "Resolved",
                reason: null,
                resolved);
            return resolved;
        }
        catch (Exception exception)
        {
            PublishOnce(
                requested,
                consumer,
                DiagnosticSeverity.Error,
                "Failed",
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

        _diagnosticsSink.Emit(new DiagnosticRecord(
            Stage: "stage/font",
            Name: "font/resolve",
            Severity: severity,
            Message: reason,
            Context: null,
            Fields: DiagnosticFields.Create(
                DiagnosticFields.Field("owner", nameof(FontPathSource)),
                DiagnosticFields.Field("consumer", consumer),
                DiagnosticFields.Field("requestedFamily", requested.Family),
                DiagnosticFields.Field("requestedWeight", DiagnosticValue.FromEnum(requested.Weight)),
                DiagnosticFields.Field("requestedStyle", DiagnosticValue.FromEnum(requested.Style)),
                DiagnosticFields.Field("resolvedFamily", resolved?.Family is null ? null : DiagnosticValue.From(resolved.Family)),
                DiagnosticFields.Field("resolvedWeight", resolved is null ? null : DiagnosticValue.FromEnum(resolved.Weight)),
                DiagnosticFields.Field("resolvedStyle", resolved is null ? null : DiagnosticValue.FromEnum(resolved.Style)),
                DiagnosticFields.Field("sourceId", resolved?.SourceId is null ? null : DiagnosticValue.From(resolved.SourceId)),
                DiagnosticFields.Field("configuredPath", resolved?.ConfiguredPath is null ? null : DiagnosticValue.From(resolved.ConfiguredPath)),
                DiagnosticFields.Field("filePath", resolved?.FilePath is null ? null : DiagnosticValue.From(resolved.FilePath)),
                DiagnosticFields.Field("faceIndex", resolved is null ? null : DiagnosticValue.From(resolved.FaceIndex)),
                DiagnosticFields.Field("outcome", outcome),
                DiagnosticFields.Field("reason", reason is null ? null : DiagnosticValue.From(reason))),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private readonly record struct ResolutionEventKey(
        string Consumer,
        string RequestedFamily,
        FontWeight RequestedWeight,
        FontStyle RequestedStyle,
        string? SourceId,
        string Outcome);

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

        return new ResolvedFont(
            resolved?.Family ?? string.Empty,
            resolved?.Weight ?? default,
            resolved?.Style ?? default,
            sourceId ?? string.Empty,
            FilePath: filePath,
            FaceIndex: resolved?.FaceIndex ?? 0,
            ConfiguredPath: configuredPath);
    }
}
