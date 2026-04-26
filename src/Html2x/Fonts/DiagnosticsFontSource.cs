using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;

namespace Html2x.Fonts;

internal sealed class DiagnosticsFontSource : IFontSource
{
    private readonly IFontSource _inner;
    private readonly DiagnosticsSession _session;
    private readonly HashSet<ResolutionEventKey> _published = [];
    private readonly object _gate = new();

    public DiagnosticsFontSource(IFontSource inner, DiagnosticsSession session)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _session = session ?? throw new ArgumentNullException(nameof(session));
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

        _session.Events.Add(new DiagnosticsEvent
        {
            Type = DiagnosticsEventType.Trace,
            Name = "font/resolve",
            Severity = severity,
            Description = reason,
            Payload = new FontResolutionPayload
            {
                Owner = nameof(FontPathSource),
                Consumer = consumer,
                RequestedFamily = requested.Family,
                RequestedWeight = requested.Weight,
                RequestedStyle = requested.Style,
                ResolvedFamily = resolved?.Family,
                ResolvedWeight = resolved?.Weight,
                ResolvedStyle = resolved?.Style,
                SourceId = resolved?.SourceId,
                ConfiguredPath = resolved?.ConfiguredPath,
                FilePath = resolved?.FilePath,
                FaceIndex = resolved?.FaceIndex,
                Outcome = outcome,
                Reason = reason
            }
        });
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
