using System.Collections.Concurrent;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
using Html2x.Files;
using Html2x.Renderers.Pdf.Drawing;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Html2x;

/// <summary>
/// Measures text using SkiaSharp and HarfBuzz with strict font resolution.
/// </summary>
public sealed class SkiaTextMeasurer : ITextMeasurer, IDisposable
{
    private readonly IFontSource _fontSource;
    private readonly IFileDirectory _fileDirectory;
    private readonly ISkiaTypefaceFactory _typefaceFactory;
    // Render-scoped caches; not shared across conversions.
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SKShaper> _shapersBySourceId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<TextMeasureKey, float> _widthBySourceAndText = new();
    private readonly ConcurrentDictionary<MetricKey, (float Ascent, float Descent)> _metricsBySourceAndSize = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<FontFaceEntry>> _facesByDirectory = new(StringComparer.OrdinalIgnoreCase);

    public SkiaTextMeasurer(IFontSource fontSource)
        : this(fontSource, new FileDirectory(), new DefaultTypefaceFactory())
    {
    }
    
    internal SkiaTextMeasurer(IFontSource fontSource, IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory)
    {
        _fontSource = fontSource ?? throw new ArgumentNullException(nameof(fontSource));
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
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
        var path = resolved.FilePath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (_fileDirectory.FileExists(path))
            {
            return _typefaceFactory.FromFile(path) ?? throw CreateFontResolutionException(
                $"Failed to load font file '{path}'.",
                resolved,
                fontKey,
                path);
        }

        if (_fileDirectory.DirectoryExists(path))
        {
            return GetTypefaceFromDirectory(fontKey, path);
            }
        }

        if (_fileDirectory.FileExists(resolved.SourceId))
        {
            return _typefaceFactory.FromFile(resolved.SourceId) ?? throw CreateFontResolutionException(
                $"Failed to load font file '{resolved.SourceId}'.",
                resolved,
                fontKey,
                resolved.SourceId);
        }

        if (_fileDirectory.DirectoryExists(resolved.SourceId))
        {
            return GetTypefaceFromDirectory(fontKey, resolved.SourceId);
        }

        throw CreateFontResolutionException(
            "Font resolution failed using the configured font path.",
            resolved,
            fontKey);
    }

    private SKTypeface GetTypefaceFromDirectory(FontKey fontKey, string directory)
    {
        var faces = _facesByDirectory.GetOrAdd(directory, dir => FontDirectoryIndex.Build(_fileDirectory, _typefaceFactory, dir));
        var best = FontDirectoryIndex.FindBestMatch(faces, fontKey);
        if (best is null)
        {
            throw CreateFontResolutionException(
                $"Font '{fontKey.Family}' not found in directory '{directory}'.",
                new ResolvedFont(fontKey.Family, fontKey.Weight, fontKey.Style, directory, directory),
                fontKey,
                directory);
        }

        if (best.FaceIndex > 0)
        {
            return _typefaceFactory.FromFile(best.Path, best.FaceIndex) ??
                throw CreateFontResolutionException(
                    $"Failed to load font file '{best.Path}' (face {best.FaceIndex}).",
                    new ResolvedFont(fontKey.Family, fontKey.Weight, fontKey.Style, best.Path, directory),
                    fontKey,
                    best.Path);
        }

        return _typefaceFactory.FromFile(best.Path) ??
            throw CreateFontResolutionException(
                $"Failed to load font file '{best.Path}'.",
                new ResolvedFont(fontKey.Family, fontKey.Weight, fontKey.Style, best.Path, directory),
                fontKey,
                best.Path);
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

    private ResolvedFont GetResolvedFont(FontKey fontKey) => _fontSource.Resolve(fontKey);

    private static SKFont CreateFont(SKTypeface typeface, float sizePt) => new(typeface, sizePt);

    private static InvalidOperationException CreateFontResolutionException(
        string message,
        ResolvedFont resolved,
        FontKey? fontKey,
        string? path = null)
    {
        var exception = new InvalidOperationException(message);
        exception.Data["DiagnosticsName"] = "FontPath";
        exception.Data["FontFamily"] = resolved.Family;
        exception.Data["FontWeight"] = resolved.Weight;
        exception.Data["FontStyle"] = resolved.Style;
        exception.Data["FontSourceId"] = resolved.SourceId;
        exception.Data["FontFilePath"] = resolved.FilePath;

        if (fontKey is not null)
        {
            exception.Data["RequestedFamily"] = fontKey.Family;
            exception.Data["RequestedWeight"] = fontKey.Weight;
            exception.Data["RequestedStyle"] = fontKey.Style;
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            exception.Data["FontResolvedPath"] = path;
        }

        return exception;
    }

    public void Dispose()
    {
        // Dispose native resources once per handle; skip default typeface.
        foreach (var shaper in _shapersBySourceId.Values)
        {
            shaper.Dispose();
        }

        var disposedHandles = new HashSet<IntPtr>();
        foreach (var typeface in _typefacesBySourceId.Values)
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

    private readonly record struct TextMeasureKey(string SourceId, float SizePt, string Text);

    private readonly record struct MetricKey(string SourceId, float SizePt);

    private sealed class DefaultTypefaceFactory : ISkiaTypefaceFactory
    {
        public SKTypeface? FromFile(string path) => SKTypeface.FromFile(path);

        public SKTypeface? FromFile(string path, int faceIndex) => SKTypeface.FromFile(path, faceIndex);

        public SKTypeface? FromFamilyName(string family, SKFontStyle style) => SKTypeface.FromFamilyName(family, style);
    }
}
