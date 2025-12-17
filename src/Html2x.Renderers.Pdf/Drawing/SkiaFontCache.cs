using System.Collections.Concurrent;
using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Files;
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
    private readonly string? _fontPath;
    private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefaces = new();
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefacesFromDirectory = new();
    private readonly Lazy<IReadOnlyList<TypefaceEntry>> _directoryFaces;

    public SkiaFontCache(string? fontPath)
        : this(fontPath, new FileDirectory())
    {
    }

    internal SkiaFontCache(string? fontPath, IFileDirectory fileDirectory)
    {
        _fontPath = string.IsNullOrWhiteSpace(fontPath) ? null : fontPath;
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _directoryFaces = new Lazy<IReadOnlyList<TypefaceEntry>>(LoadDirectoryFaces, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SKTypeface GetTypeface(FontKey key)
    {
        if (_fontPath is not null && _fileDirectory.FileExists(_fontPath))
        {
            return _typefacesByPath.GetOrAdd(_fontPath, path =>
            {
                var fromFile = SKTypeface.FromFile(path);
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
            var tf = SKTypeface.FromFamilyName(family, style);
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

        var wantsItalic = key.Style is FontStyle.Italic or FontStyle.Oblique;
        var requestedWeight = (int)key.Weight;
        var familyCandidates = GetFamilyCandidates(key.Family);

        foreach (var family in familyCandidates)
        {
            var best = FindBestMatchInDirectory(faces, family, requestedWeight, wantsItalic);
            if (best is not null)
            {
                return best;
            }
        }

        return null;
    }

    private static SKTypeface? FindBestMatchInDirectory(
        IReadOnlyList<TypefaceEntry> faces,
        string family,
        int requestedWeight,
        bool wantsItalic)
    {
        TypefaceEntry? best = null;
        var bestSlantMatch = false;
        var bestWeightDistance = int.MaxValue;

        for (var i = 0; i < faces.Count; i++)
        {
            var entry = faces[i];
            if (!string.Equals(entry.Family, family, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var slantMatch = entry.IsItalic == wantsItalic;
            var weightDistance = Math.Abs(entry.Weight - requestedWeight);

            if (best is null)
            {
                best = entry;
                bestSlantMatch = slantMatch;
                bestWeightDistance = weightDistance;
                continue;
            }

            if (bestSlantMatch != slantMatch)
            {
                if (slantMatch)
                {
                    best = entry;
                    bestSlantMatch = true;
                    bestWeightDistance = weightDistance;
                }

                continue;
            }

            if (weightDistance < bestWeightDistance)
            {
                best = entry;
                bestWeightDistance = weightDistance;
                continue;
            }

            if (weightDistance == bestWeightDistance)
            {
                var pathComparison = StringComparer.OrdinalIgnoreCase.Compare(entry.Path, best.Path);
                if (pathComparison < 0 || (pathComparison == 0 && entry.FaceIndex < best.FaceIndex))
                {
                    best = entry;
                    bestWeightDistance = weightDistance;
                }
            }
        }

        return best?.Typeface;
    }

    private IReadOnlyList<TypefaceEntry> LoadDirectoryFaces()
    {
        if (_fontPath is null || !_fileDirectory.DirectoryExists(_fontPath))
        {
            return [];
        }

        var files = SkiaFontFileDiscovery.ListFontFiles(_fileDirectory, _fontPath);
        if (files.Count == 0)
        {
            return [];
        }

        var faces = new List<TypefaceEntry>(capacity: files.Count);

        foreach (var file in files)
        {
            var ext = _fileDirectory.GetExtension(file);
            if (string.Equals(ext, ".ttc", StringComparison.OrdinalIgnoreCase))
            {
                LoadCollectionFaces(file, faces);
                continue;
            }

            var tf = SKTypeface.FromFile(file);
            if (tf is null)
            {
                continue;
            }

            faces.Add(TypefaceEntry.FromSingleFace(file, tf));
        }

        return faces;
    }

    private static void LoadCollectionFaces(string file, List<TypefaceEntry> faces)
    {
        for (var index = 0; ; index++)
        {
            var tf = SKTypeface.FromFile(file, index);
            if (tf is null)
            {
                break;
            }

            faces.Add(TypefaceEntry.FromCollectionFace(file, index, tf));
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _typefaces)
        {
            kvp.Value.Dispose();
        }

        foreach (var kvp in _typefacesByPath)
        {
            kvp.Value.Dispose();
        }

        foreach (var kvp in _typefacesFromDirectory)
        {
            // Values are owned by _directoryFaces; do not dispose here.
            _ = kvp;
        }

        var faces = _directoryFaces.IsValueCreated ? _directoryFaces.Value : null;
        if (faces is not null)
        {
            for (var i = 0; i < faces.Count; i++)
            {
                faces[i].Typeface.Dispose();
            }
        }
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

    private sealed record TypefaceEntry(string Path, int FaceIndex, SKTypeface Typeface, string Family, int Weight, bool IsItalic)
    {
        public static TypefaceEntry FromSingleFace(string path, SKTypeface typeface)
        {
            return new TypefaceEntry(
                path,
                FaceIndex: 0,
                typeface,
                typeface.FamilyName ?? string.Empty,
                typeface.FontWeight,
                typeface.IsItalic || typeface.FontSlant != SKFontStyleSlant.Upright);
        }

        public static TypefaceEntry FromCollectionFace(string path, int faceIndex, SKTypeface typeface)
        {
            return new TypefaceEntry(
                path,
                faceIndex,
                typeface,
                typeface.FamilyName ?? string.Empty,
                typeface.FontWeight,
                typeface.IsItalic || typeface.FontSlant != SKFontStyleSlant.Upright);
        }
    }
}
