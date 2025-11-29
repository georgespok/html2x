using System.Text;

namespace Html2x.Renderers.Pdf;

/// <summary>
/// Loads image bytes from data URIs or scoped local file paths.
/// </summary>
internal static class ImageLoader
{
    public static byte[]? Load(string src, string htmlDirectory)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return null;
        }

        if (IsDataUri(src))
        {
            return DecodeDataUri(src);
        }

        var fullPath = Path.GetFullPath(Path.Combine(htmlDirectory, src));
        if (!IsWithinScope(fullPath, htmlDirectory))
        {
            return null;
        }

        return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
    }

    private static bool IsWithinScope(string fullPath, string baseDir)
    {
        var basePath = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDataUri(string src) =>
        src.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static byte[]? DecodeDataUri(string src)
    {
        // Expected form: data:[<mediatype>][;base64],<data>
        var commaIndex = src.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0 || commaIndex == src.Length - 1)
        {
            return null;
        }

        var metadata = src.Substring(5, commaIndex - 5); // skip "data:"
        var payload = src.Substring(commaIndex + 1);
        var isBase64 = metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);

        try
        {
            return isBase64
                ? Convert.FromBase64String(payload)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
        }
        catch
        {
            return null;
        }
    }
}
