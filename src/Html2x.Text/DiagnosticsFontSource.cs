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
        var configuredPath = exception.Data["FontConfiguredPath"] as string;
        var filePath = exception.Data["FontFilePath"] as string;
        var sourceId = exception.Data["FontSourceId"] as string
            ?? exception.Data["FontResolvedPath"] as string
            ?? configuredPath;

        if (string.IsNullOrWhiteSpace(sourceId) &&
            string.IsNullOrWhiteSpace(configuredPath) &&
            string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        return new ResolvedFont(
            exception.Data["FontFamily"] as string ?? string.Empty,
            exception.Data["FontWeight"] is FontWeight weight ? weight : default,
            exception.Data["FontStyle"] is FontStyle style ? style : default,
            sourceId ?? string.Empty,
            FilePath: filePath,
            FaceIndex: exception.Data["FontFaceIndex"] is int faceIndex ? faceIndex : 0,
            ConfiguredPath: configuredPath);
    }
}
