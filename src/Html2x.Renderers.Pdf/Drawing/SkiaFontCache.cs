using System.Collections.Concurrent;
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
internal sealed class SkiaFontCache(string? fontPath) : IDisposable
{
    private readonly string? _fontPath = string.IsNullOrWhiteSpace(fontPath) ? null : fontPath;
    private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefaces = new();
    private readonly ConcurrentDictionary<string, SKTypeface> _typefacesByPath = new(StringComparer.OrdinalIgnoreCase);

    public SKTypeface GetTypeface(FontKey key)
    {
        if (_fontPath is not null && File.Exists(_fontPath))
        {
            return _typefacesByPath.GetOrAdd(_fontPath, path =>
            {
                var fromFile = SKTypeface.FromFile(path);
                return fromFile ?? SKTypeface.Default;
            });
        }

        return _typefaces.GetOrAdd(key, static k =>
        {
            var style = new SKFontStyle(MapWeight(k.Weight), SKFontStyleWidth.Normal, MapSlant(k.Style));
            var familyCandidates = GetFamilyCandidates(k.Family);

            foreach (var family in familyCandidates)
            {
                var tf = SKTypeface.FromFamilyName(family, style);
                if (tf is not null)
                {
                    return tf;
                }
            }

            return SKTypeface.Default;
        });
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
