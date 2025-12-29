using System.Collections.Concurrent;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Layout.Text;
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
    private readonly ConcurrentDictionary<string, SKTypeface> _typefaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SKShaper> _shapers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<TextMeasureKey, float> _widthCache = new();
    private readonly ConcurrentDictionary<MetricKey, (float Ascent, float Descent)> _metricsCache = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<FontFaceEntry>> _directoryFaces = new(StringComparer.OrdinalIgnoreCase);

    public SkiaTextMeasurer(IFontSource fontSource)
        : this(fontSource, new LocalFileDirectory(), new DefaultTypefaceFactory())
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

        var resolved = _fontSource.Resolve(font);
        var sourceId = ValidateSourceId(resolved);
        var key = new TextMeasureKey(sourceId, sizePt, text);

        return _widthCache.GetOrAdd(key, _ => MeasureWidthCore(font, sizePt, text, resolved));
    }

    public (float Ascent, float Descent) GetMetrics(FontKey font, float sizePt)
    {
        var resolved = _fontSource.Resolve(font);
        var sourceId = ValidateSourceId(resolved);
        var key = new MetricKey(sourceId, sizePt);

        return _metricsCache.GetOrAdd(key, _ => MeasureMetricsCore(font, sizePt, resolved));
    }

    private float MeasureWidthCore(FontKey fontKey, float sizePt, string text, ResolvedFont resolved)
    {
        var typeface = ResolveTypeface(fontKey, resolved);
        var shaper = _shapers.GetOrAdd(resolved.SourceId, _ => new SKShaper(typeface));

        using var font = new SKFont(typeface, sizePt);
        var result = shaper.Shape(text, font);
        return result.Width;
    }

    private (float Ascent, float Descent) MeasureMetricsCore(FontKey fontKey, float sizePt, ResolvedFont resolved)
    {
        var typeface = ResolveTypeface(fontKey, resolved);
        using var font = new SKFont(typeface, sizePt);
        var metrics = font.Metrics;
        var ascent = Math.Abs(metrics.Ascent);
        var descent = Math.Abs(metrics.Descent);
        return (ascent, descent);
    }

    private SKTypeface ResolveTypeface(FontKey fontKey, ResolvedFont resolved)
    {
        return _typefaces.GetOrAdd(resolved.SourceId, _ => LoadTypeface(fontKey, resolved));
    }

    private SKTypeface LoadTypeface(FontKey fontKey, ResolvedFont resolved)
    {
        var path = resolved.FilePath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (_fileDirectory.FileExists(path))
            {
                return _typefaceFactory.FromFile(path) ?? throw new InvalidOperationException($"Failed to load font file '{path}'.");
            }

            if (_fileDirectory.DirectoryExists(path))
            {
                return ResolveFromDirectory(fontKey, path);
            }
        }

        if (_fileDirectory.FileExists(resolved.SourceId))
        {
            return _typefaceFactory.FromFile(resolved.SourceId) ?? throw new InvalidOperationException($"Failed to load font file '{resolved.SourceId}'.");
        }

        if (_fileDirectory.DirectoryExists(resolved.SourceId))
        {
            return ResolveFromDirectory(fontKey, resolved.SourceId);
        }

        throw new InvalidOperationException($"Font '{resolved.Family}' could not be resolved from the configured font source.");
    }

    private SKTypeface ResolveFromDirectory(FontKey fontKey, string directory)
    {
        var faces = _directoryFaces.GetOrAdd(directory, dir => FontDirectoryIndex.Build(_fileDirectory, _typefaceFactory, dir));
        var best = FontDirectoryIndex.FindBestMatch(faces, fontKey);
        if (best is null)
        {
            throw new InvalidOperationException($"Font '{fontKey.Family}' not found in directory '{directory}'.");
        }

        if (best.FaceIndex > 0)
        {
            return _typefaceFactory.FromFile(best.Path, best.FaceIndex) ??
                throw new InvalidOperationException($"Failed to load font file '{best.Path}' (face {best.FaceIndex}).");
        }

        return _typefaceFactory.FromFile(best.Path) ??
            throw new InvalidOperationException($"Failed to load font file '{best.Path}'.");
    }

    private static string ValidateSourceId(ResolvedFont resolved)
    {
        if (string.IsNullOrWhiteSpace(resolved.SourceId))
        {
            throw new InvalidOperationException("Resolved font source id cannot be empty.");
        }

        return resolved.SourceId;
    }

    public void Dispose()
    {
        foreach (var shaper in _shapers.Values)
        {
            shaper.Dispose();
        }

        var disposedHandles = new HashSet<IntPtr>();
        foreach (var typeface in _typefaces.Values)
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

    private sealed class LocalFileDirectory : IFileDirectory
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public IEnumerable<string> EnumerateFiles(string directory, string searchPattern, bool recursive)
        {
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(directory, searchPattern, option);
        }

        public string GetExtension(string path) => Path.GetExtension(path);
    }

    private sealed class DefaultTypefaceFactory : ISkiaTypefaceFactory
    {
        public SKTypeface? FromFile(string path) => SKTypeface.FromFile(path);

        public SKTypeface? FromFile(string path, int faceIndex) => SKTypeface.FromFile(path, faceIndex);

        public SKTypeface? FromFamilyName(string family, SKFontStyle style) => SKTypeface.FromFamilyName(family, style);
    }
}
