using System.Collections.Concurrent;
using Html2x.Abstractions.File;
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
/// Support an explicit file-backed font via the renderer option <c>PdfOptions.FontPath</c> when provided, and fall
/// back to system font resolution otherwise.
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
    private readonly string? _fontPath;
    private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefaces = new();
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefacesFromDirectory = new();
    private readonly Lazy<IReadOnlyList<FontFaceEntry>> _directoryFaces;

    internal SkiaFontCache(string? fontPath, IFileDirectory fileDirectory)
        : this(fontPath, fileDirectory, new SkiaTypefaceFactory())
    {
    }

    internal SkiaFontCache(string? fontPath, IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory)
    {
        _fontPath = string.IsNullOrWhiteSpace(fontPath) ? null : fontPath;
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
        _directoryFaces = new Lazy<IReadOnlyList<FontFaceEntry>>(LoadDirectoryFaces, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SKTypeface GetTypeface(FontKey key)
    {
        if (_fontPath is not null && _fileDirectory.FileExists(_fontPath))
        {
            return _typefacesByPath.GetOrAdd(_fontPath, path =>
            {
                var fromFile = _typefaceFactory.FromFile(path);
                return fromFile ?? SKTypeface.Default;
            });
        }

        if (_fontPath is not null && _fileDirectory.DirectoryExists(_fontPath))
        {
            return _typefacesFromDirectory.GetOrAdd(key, ResolveFromDirectoryOrFallbackToSystem);
        }

        return _typefaces.GetOrAdd(key, ResolveFromSystem);
    }

    private SKTypeface ResolveFromDirectoryOrFallbackToSystem(FontKey key)
    {
        var fromDirectory = TryResolveFromDirectory(key);
        if (fromDirectory is not null)
        {
            return fromDirectory;
        }

        return _typefaces.GetOrAdd(key, ResolveFromSystem);
    }

    private SKTypeface ResolveFromSystem(FontKey key)
    {
        var style = new SKFontStyle(MapWeight(key.Weight), SKFontStyleWidth.Normal, MapSlant(key.Style));
        var familyCandidates = GetFamilyCandidates(key.Family);

        foreach (var family in familyCandidates)
        {
            var tf = _typefaceFactory.FromFamilyName(family, style);
            if (tf is not null)
            {
                return tf;
            }
        }

        return SKTypeface.Default;
    }

    private SKTypeface? TryResolveFromDirectory(FontKey key)
    {
        var faces = _directoryFaces.Value;
        if (faces.Count == 0)
        {
            return null;
        }

        var best = FontDirectoryIndex.FindBestMatch(faces, key);
        if (best is null)
        {
            return null;
        }

        if (best.FaceIndex > 0)
        {
            return _typefaceFactory.FromFile(best.Path, best.FaceIndex);
        }

        return _typefaceFactory.FromFile(best.Path);
    }

    private IReadOnlyList<FontFaceEntry> LoadDirectoryFaces()
    {
        if (_fontPath is null || !_fileDirectory.DirectoryExists(_fontPath))
        {
            return [];
        }

        return FontDirectoryIndex.Build(_fileDirectory, _typefaceFactory, _fontPath);
    }

    public void Dispose()
    {
        var disposedHandles = new HashSet<IntPtr>();

        DisposeTypefaces(_typefaces.Values, disposedHandles);
        DisposeTypefaces(_typefacesByPath.Values, disposedHandles);

        // No directory typefaces to dispose because the index stores metadata only.
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

    private static IEnumerable<string> GetFamilyCandidates(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            yield return SKTypeface.Default.FamilyName;
            yield break;
        }

        yield return family;

        if (string.Equals(family, "Arial", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Liberation Sans";
            yield return "Helvetica";
        }

        yield return SKTypeface.Default.FamilyName;
    }

    private static SKFontStyleWeight MapWeight(FontWeight weight) =>
        weight switch
        {
            FontWeight.W100 => SKFontStyleWeight.Thin,
            FontWeight.W200 => SKFontStyleWeight.ExtraLight,
            FontWeight.W300 => SKFontStyleWeight.Light,
            FontWeight.W400 => SKFontStyleWeight.Normal,
            FontWeight.W500 => SKFontStyleWeight.Medium,
            FontWeight.W600 => SKFontStyleWeight.SemiBold,
            FontWeight.W700 => SKFontStyleWeight.Bold,
            FontWeight.W800 => SKFontStyleWeight.ExtraBold,
            FontWeight.W900 => SKFontStyleWeight.Black,
            _ => SKFontStyleWeight.Normal
        };

    private static SKFontStyleSlant MapSlant(FontStyle style) =>
        style switch
        {
            FontStyle.Italic => SKFontStyleSlant.Italic,
            FontStyle.Oblique => SKFontStyleSlant.Oblique,
            _ => SKFontStyleSlant.Upright
        };

}
