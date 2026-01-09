using System.Text;
using Html2x.Abstractions.Images;

namespace Html2x;

/// <summary>
/// Loads image bytes from data URIs or scoped file paths and enforces max byte size.
/// </summary>
public sealed class FileSystemImageProvider : IImageProvider
{
    public ImageLoadResult Load(string src, string baseDirectory, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return Missing(src);
        }

        try
        {
            if (IsDataUri(src))
            {
                var bytes = DecodeDataUri(src);
                if (bytes is null)
                {
                    return Missing(src);
                }

                if (bytes.LongLength > maxBytes)
                {
                    return Oversize(src);
                }

                return Ok(src, 0, 0);
            }

            var full = Path.GetFullPath(Path.Combine(baseDirectory, src));
            if (!IsWithinScope(full, baseDirectory) || !File.Exists(full))
            {
                return Missing(src);
            }

            var length = new FileInfo(full).Length;
            if (length > maxBytes)
            {
                return Oversize(src);
            }

            return Ok(src, 0, 0);
        }
        catch
        {
            return Missing(src);
        }
    }

    private static bool IsDataUri(string src) => src.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static byte[]? DecodeDataUri(string src)
    {
        var commaIndex = src.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0 || commaIndex == src.Length - 1)
        {
            return null;
        }

        var metadata = src.Substring(5, commaIndex - 5);
        var payload = src.Substring(commaIndex + 1);
        var isBase64 = metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);

        return isBase64
            ? Convert.FromBase64String(payload)
            : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
    }

    private static bool IsWithinScope(string fullPath, string baseDir)
    {
        var basePath = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                       Path.DirectorySeparatorChar;
        return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }

    private static ImageLoadResult Missing(string src) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Missing,
            IntrinsicSizePx = new Html2x.Abstractions.Measurements.Units.SizePx(0, 0)
        };

    private static ImageLoadResult Oversize(string src) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Oversize,
            IntrinsicSizePx = new Html2x.Abstractions.Measurements.Units.SizePx(0, 0)
        };

    private static ImageLoadResult Ok(string src, double width, double height) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            IntrinsicSizePx = new Html2x.Abstractions.Measurements.Units.SizePx(width, height)
        };
}
