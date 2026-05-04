using System.Collections.Concurrent;
using Html2x.RenderModel;
using Html2x.Text;
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
/// Normalize font selection across fragments by mapping resolved text run font facts to stable Skia typefaces.
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
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesBySourceId = new(StringComparer.OrdinalIgnoreCase);

    internal SkiaFontCache(
        IFileDirectory fileDirectory,
        ISkiaTypefaceFactory typefaceFactory)
    {
        _fileDirectory = fileDirectory ?? throw new ArgumentNullException(nameof(fileDirectory));
        _typefaceFactory = typefaceFactory ?? throw new ArgumentNullException(nameof(typefaceFactory));
    }

    public SKTypeface GetTypeface(TextRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var resolved = run.ResolvedFont ?? throw CreateMissingResolvedFontException(run);

        return _typefacesBySourceId.GetOrAdd(
            resolved.SourceId,
            _ => LoadResolvedTypeface(run.Font, resolved));
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

        var typeface = LoadTypeface(path, resolved.FaceIndex);

        return typeface ?? throw CreateFontResolutionException(
            CreateFontLoadFailureMessage(path, resolved.FaceIndex),
            key,
            resolved,
            path);
    }

    private SKTypeface? LoadTypeface(string path, int faceIndex) =>
        faceIndex > 0
            ? _typefaceFactory.FromFile(path, faceIndex)
            : _typefaceFactory.FromFile(path);

    public void Dispose()
    {
        var disposedHandles = new HashSet<IntPtr>();

        DisposeTypefaces(_typefacesBySourceId.Values, disposedHandles);
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

    private static FontResolutionException CreateFontResolutionException(
        string message,
        FontKey requested,
        ResolvedFont resolved,
        string? path)
    {
        return new FontResolutionException(
            message,
            requested,
            resolved,
            resolvedPath: path);
    }

    private static FontResolutionException CreateMissingResolvedFontException(TextRun run)
    {
        return new FontResolutionException(
            "TextRun.ResolvedFont is required before PDF rendering. Build renderer inputs through layout geometry or provide resolved font facts on manually constructed text runs.",
            run.Font,
            text: run.Text);
    }
}
