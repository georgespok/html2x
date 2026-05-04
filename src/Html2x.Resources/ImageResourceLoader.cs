using System.Text;
using Html2x.RenderModel;
using SkiaSharp;

namespace Html2x.Resources;


internal static class ImageResourceLoader
{
    public static ImageResourceResult Load(string src, string? baseDirectory, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return Create(src, ImageResourceStatus.Missing);
        }

        var bytesResult = TryLoadBytes(src, baseDirectory);
        if (bytesResult.Status != ImageResourceStatus.Ok || bytesResult.Bytes is null)
        {
            return Create(src, bytesResult.Status);
        }

        var bytes = bytesResult.Bytes;
        if (bytes.LongLength > maxBytes)
        {
            return Create(src, ImageResourceStatus.Oversize);
        }

        var intrinsicSize = DecodeIntrinsicSize(bytes);
        if (intrinsicSize is null)
        {
            return Create(src, ImageResourceStatus.DecodeFailed);
        }

        return Ok(src, bytes, intrinsicSize.Value);
    }

    public static string ResolveBaseDirectory(string? baseDirectory)
    {
        return string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(baseDirectory);
    }

    private static ImageBytesResult TryLoadBytes(string src, string? baseDirectory)
    {
        if (IsDataUri(src))
        {
            return DecodeDataUri(src);
        }

        var resolvedBaseDirectory = ResolveBaseDirectory(baseDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(resolvedBaseDirectory, src));
        if (!IsWithinScope(fullPath, resolvedBaseDirectory))
        {
            return ImageBytesResult.Failed(ImageResourceStatus.OutOfScope);
        }

        if (!File.Exists(fullPath))
        {
            return ImageBytesResult.Failed(ImageResourceStatus.Missing);
        }

        try
        {
            return ImageBytesResult.Ok(File.ReadAllBytes(fullPath));
        }
        catch (IOException)
        {
            return ImageBytesResult.Failed(ImageResourceStatus.Missing);
        }
        catch (UnauthorizedAccessException)
        {
            return ImageBytesResult.Failed(ImageResourceStatus.Missing);
        }
    }

    private static SizePx? DecodeIntrinsicSize(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        return codec is null
            ? null
            : new SizePx(codec.Info.Width, codec.Info.Height);
    }

    private static bool IsDataUri(string src) =>
        src.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static ImageBytesResult DecodeDataUri(string src)
    {
        var commaIndex = src.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0 || commaIndex == src.Length - 1)
        {
            return ImageBytesResult.Failed(ImageResourceStatus.InvalidDataUri);
        }

        var metadata = src.Substring(5, commaIndex - 5);
        var payload = src.Substring(commaIndex + 1);
        var isBase64 = metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);

        try
        {
            return ImageBytesResult.Ok(isBase64
                ? Convert.FromBase64String(payload)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload)));
        }
        catch (FormatException)
        {
            return ImageBytesResult.Failed(ImageResourceStatus.InvalidDataUri);
        }
    }

    private static bool IsWithinScope(string fullPath, string baseDirectory)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var basePath = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        return fullPath.StartsWith(basePath, comparison);
    }

    private static ImageResourceResult Create(string src, ImageResourceStatus status) =>
        new()
        {
            Src = src,
            Status = status,
            IntrinsicSizePx = new SizePx(0d, 0d)
        };

    private static ImageResourceResult Ok(string src, byte[] bytes, SizePx intrinsicSizePx) =>
        new()
        {
            Src = src,
            Status = ImageResourceStatus.Ok,
            Bytes = bytes,
            IntrinsicSizePx = intrinsicSizePx
        };

    private readonly record struct ImageBytesResult(ImageResourceStatus Status, byte[]? Bytes)
    {
        public static ImageBytesResult Ok(byte[] bytes) => new(ImageResourceStatus.Ok, bytes);

        public static ImageBytesResult Failed(ImageResourceStatus status) => new(status, null);
    }
}
