using System.Collections.Concurrent;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Fonts;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Provides a small, render-scoped cache for <see cref="SKTypeface"/> instances used by the Skia PDF renderer.
/// </summary>
/// <remarks>
/// Purpose:
/// <list type="bullet">
/// <item>
/// Avoid repeatedly creating <see cref="SKTypeface"/> objects during a render pass. Typeface creation can be
/// relatively expensive and each instance wraps native resources that should be disposed deterministically.
/// </item>
/// <item>
/// Normalize font selection across fragments by mapping a layout <see cref="FontKey"/> to a stable Skia typeface.
/// </item>
/// <item>
/// Consume converter-owned resolved font output when available, and delegate direct renderer-only fallback to
/// <see cref="RendererFallbackFontResolver"/> when no shared font source exists.
/// </item>
/// </list>
///
/// Lifecycle:
/// Create one cache per conversion and dispose it when rendering completes. This keeps native resources bounded and
/// avoids cross-request global state.
/// </remarks>
internal sealed class SkiaFontCache : IDisposable
{
    private readonly IFileDirectory _fileDirectory;
    private readonly ISkiaTypefaceFactory _typefaceFactory;
    private readonly IFontSource? _fontSource;
    private readonly RendererFallbackFontResolver _fallbackFontResolver;
    private readonly ConcurrentDictionary<FontKey, ResolvedFont> _resolvedFonts = new();
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesBySourceId = new(StringComparer.OrdinalIgnoreCase);

    internal SkiaFontCache(string? fontPath, IFileDirectory fileDirectory)
        : this(fontPath, fileDirectory, typefaceFactory: new SkiaTypefaceFactory(), fontSource: null)
    {
    }

    internal SkiaFontCache(string? fontPath, IFileDirectory fileDirectory, IFontSource? fontSource)
        : this(fontPath, fileDirectory, new SkiaTypefaceFactory(), fontSource)
    {
    }

    internal SkiaFontCache(
        string? fontPath,
        IFileDirectory fileDirectory,
        ISkiaTypefaceFactory typefaceFactory,
        IFontSource? fontSource = null)
    {
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
        _fontSource = fontSource;
        _fallbackFontResolver = new RendererFallbackFontResolver(fontPath, _fileDirectory, _typefaceFactory);
    }

    public SKTypeface GetTypeface(FontKey key)
    {
        if (_fontSource is not null)
        {
            var resolved = _resolvedFonts.GetOrAdd(key, static (fontKey, source) => source.Resolve(fontKey, nameof(SkiaFontCache)), _fontSource);
            return _typefacesBySourceId.GetOrAdd(resolved.SourceId, _ => LoadResolvedTypeface(key, resolved));
        }

        return _fallbackFontResolver.GetTypeface(key);
    }

    public SKTypeface GetTypeface(TextRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return run.ResolvedFont is null
            ? GetTypeface(run.Font)
            : _typefacesBySourceId.GetOrAdd(run.ResolvedFont.SourceId, _ => LoadResolvedTypeface(run.Font, run.ResolvedFont));
    }

    private SKTypeface LoadResolvedTypeface(FontKey key, ResolvedFont resolved)
    {
        var path = resolved.FilePath;
        if (string.IsNullOrWhiteSpace(path) || !_fileDirectory.FileExists(path))
        {
            throw CreateFontResolutionException(
                "Resolved font did not provide a usable file path.",
                key,
                resolved,
                path);
        }

        var typeface = resolved.FaceIndex > 0
            ? _typefaceFactory.FromFile(path, resolved.FaceIndex)
            : _typefaceFactory.FromFile(path);

        return typeface ?? throw CreateFontResolutionException(
            resolved.FaceIndex > 0
                ? $"Failed to load font file '{path}' (face {resolved.FaceIndex})."
                : $"Failed to load font file '{path}'.",
            key,
            resolved,
            path);
    }

    public void Dispose()
    {
        var disposedHandles = new HashSet<IntPtr>();

        DisposeTypefaces(_typefacesBySourceId.Values, disposedHandles);
        _fallbackFontResolver.Dispose();
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

    private static InvalidOperationException CreateFontResolutionException(
        string message,
        FontKey requested,
        ResolvedFont resolved,
        string? path)
    {
        var exception = new InvalidOperationException(message);
        exception.Data["DiagnosticsName"] = "FontPath";
        exception.Data["RequestedFamily"] = requested.Family;
        exception.Data["RequestedWeight"] = requested.Weight;
        exception.Data["RequestedStyle"] = requested.Style;
        exception.Data["FontFamily"] = resolved.Family;
        exception.Data["FontWeight"] = resolved.Weight;
        exception.Data["FontStyle"] = resolved.Style;
        exception.Data["FontSourceId"] = resolved.SourceId;
        exception.Data["FontConfiguredPath"] = resolved.ConfiguredPath;
        exception.Data["FontFilePath"] = resolved.FilePath;
        exception.Data["FontFaceIndex"] = resolved.FaceIndex;

        if (!string.IsNullOrWhiteSpace(path))
        {
            exception.Data["FontResolvedPath"] = path;
        }

        return exception;
    }

}
