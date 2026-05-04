using System.Collections.Concurrent;
using Html2x.RenderModel;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Html2x.Text;

/// <summary>
/// Measures text using SkiaSharp and HarfBuzz with strict font resolution.
/// </summary>
public sealed class SkiaTextMeasurer : ITextMeasurer, IDisposable
{
    private readonly IFontSource _fontSource;
    private readonly IFileDirectory _fileDirectory;
    private readonly ISkiaTypefaceFactory _typefaceFactory;
    // Render-scoped caches; not shared across conversions.
    private readonly ConcurrentDictionary<FontKey, ResolvedFont> _resolvedFontsByRequest = new();
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SKShaper> _shapersBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<TextMeasureKey, float> _widthBySourceAndText = new();
    private readonly ConcurrentDictionary<MetricKey, (float Ascent, float Descent)> _metricsBySourceAndSize = new();

    public SkiaTextMeasurer(IFontSource fontSource)
        : this(fontSource, new FileDirectory(), new SkiaTypefaceFactory())
    {
    }

    internal SkiaTextMeasurer(IFontSource fontSource, IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory)
    {
        _fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
    }

    public TextMeasurement Measure(FontKey font, float sizePt, string text)
    {
        var resolved = GetResolvedFont(font);
        var sourceId = GetRequiredSourceId(resolved);
        var width = string.IsNullOrEmpty(text)
            ? 0f
            : _widthBySourceAndText.GetOrAdd(
                new TextMeasureKey(sourceId, sizePt, text),
                _ => MeasureWidthUsingShaper(font, sizePt, text, resolved));
        var metrics = _metricsBySourceAndSize.GetOrAdd(
            new MetricKey(sourceId, sizePt),
            _ => MeasureMetricsFromFont(font, sizePt, resolved));

        return new TextMeasurement(width, metrics.Ascent, metrics.Descent, resolved);
    }

    public float MeasureWidth(FontKey font, float sizePt, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        var resolved = GetResolvedFont(font);
        var sourceId = GetRequiredSourceId(resolved);
        var key = new TextMeasureKey(sourceId, sizePt, text);

        return _widthBySourceAndText.GetOrAdd(key, _ => MeasureWidthUsingShaper(font, sizePt, text, resolved));
    }

    public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
    {
        var resolved = GetResolvedFont(font);
        var sourceId = GetRequiredSourceId(resolved);
        var key = new MetricKey(sourceId, sizePt);

        return _metricsBySourceAndSize.GetOrAdd(key, _ => MeasureMetricsFromFont(font, sizePt, resolved));
    }

    private float MeasureWidthUsingShaper(FontKey fontKey, float sizePt, string text, ResolvedFont resolved)
    {
        var typeface = GetTypefaceForResolvedFont(fontKey, resolved);
        var shaper = _shapersBySourceId.GetOrAdd(resolved.SourceId, _ => new SKShaper(typeface));

        using var font = CreateFont(typeface, sizePt);
        var result = shaper.Shape(text, font);
        return result.Width;
    }

    private (float Ascent, float Descent) MeasureMetricsFromFont(FontKey fontKey, float sizePt, ResolvedFont resolved)
    {
        var typeface = GetTypefaceForResolvedFont(fontKey, resolved);
        using var font = CreateFont(typeface, sizePt);
        var metrics = font.Metrics;
        var ascent = Math.Abs(metrics.Ascent);
        var descent = Math.Abs(metrics.Descent);
        return (ascent, descent);
    }

    private SKTypeface GetTypefaceForResolvedFont(FontKey fontKey, ResolvedFont resolved)
    {
        return _typefacesBySourceId.GetOrAdd(resolved.SourceId, _ => LoadTypefaceForResolvedFont(fontKey, resolved));
    }

    private SKTypeface LoadTypefaceForResolvedFont(FontKey fontKey, ResolvedFont resolved)
    {
        var path = GetExistingResolvedPath(resolved);
        if (path is not null)
        {
            return LoadTypefaceFromPath(fontKey, resolved, path);
        }

        throw CreateFontResolutionException(
            "Font resolution failed using the configured font path.",
            resolved,
            fontKey);
    }

    private string? GetExistingResolvedPath(ResolvedFont resolved)
    {
        if (!string.IsNullOrWhiteSpace(resolved.FilePath) &&
            _fileDirectory.FileExists(resolved.FilePath))
        {
            return resolved.FilePath;
        }

        if (_fileDirectory.FileExists(resolved.SourceId))
        {
            return resolved.SourceId;
        }

        return null;
    }

    private SKTypeface LoadTypefaceFromPath(FontKey fontKey, ResolvedFont resolved, string path)
    {
        var typeface = resolved.FaceIndex > 0
            ? _typefaceFactory.FromFile(path, resolved.FaceIndex)
            : _typefaceFactory.FromFile(path);

        return typeface ?? throw CreateFontResolutionException(
            CreateFontLoadFailureMessage(path, resolved.FaceIndex),
            resolved,
            fontKey,
            path);
    }

    private static string GetRequiredSourceId(ResolvedFont resolved)
    {
        if (string.IsNullOrWhiteSpace(resolved.SourceId))
        {
            throw CreateFontResolutionException(
                "Resolved font source id cannot be empty.",
                resolved,
                fontKey: null);
        }

        return resolved.SourceId;
    }

    private ResolvedFont GetResolvedFont(FontKey fontKey) =>
        _resolvedFontsByRequest.GetOrAdd(
            fontKey,
            static (key, source) => source.Resolve(key, nameof(SkiaTextMeasurer)),
            _fontSource);

    private static SKFont CreateFont(SKTypeface typeface, float sizePt) => new(typeface, sizePt);

    private static FontResolutionException CreateFontResolutionException(
        string message,
        ResolvedFont resolved,
        FontKey? fontKey,
        string? path = null)
    {
        return new FontResolutionException(
            message,
            fontKey,
            resolved,
            resolvedPath: path);
    }

    public void Dispose()
    {
        // Dispose native resources once per handle; skip default typeface.
        foreach (var shaper in _shapersBySourceId.Values)
        {
            shaper.Dispose();
        }

        DisposeTypefaces(_typefacesBySourceId.Values, []);
    }

    private static void DisposeTypefaces(IEnumerable<SKTypeface> typefaces, HashSet<IntPtr> disposedHandles)
    {
        foreach (var typeface in typefaces)
        {
            if (IsDefaultTypeface(typeface))
            {
                continue;
            }

            if (!disposedHandles.Add(typeface.Handle))
            {
                continue;
            }

            typeface.Dispose();
        }
    }

    private static bool IsDefaultTypeface(SKTypeface typeface)
    {
        return ReferenceEquals(typeface, SKTypeface.Default) || typeface.Handle == SKTypeface.Default.Handle;
    }

    private static string CreateFontLoadFailureMessage(string path, int faceIndex) =>
        faceIndex > 0
            ? $"Failed to load font file '{path}' (face {faceIndex})."
            : $"Failed to load font file '{path}'.";

    private readonly record struct TextMeasureKey(string SourceId, float SizePt, string Text);

    private readonly record struct MetricKey(string SourceId, float SizePt);
}
