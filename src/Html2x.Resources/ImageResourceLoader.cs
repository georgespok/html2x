using System.Text;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using SkiaSharp;

namespace Html2x.Resources;


internal static class ImageResourceLoader
{
    public static ImageResourceMetadataResult LoadMetadata(string src, string? baseDirectory, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return CreateMetadata(src, ImageLoadStatus.Missing);
        }

        if (IsDataUri(src))
        {
            var bytesResult = DecodeDataUri(src);
            return CreateMetadataFromBytes(src, bytesResult, maxBytes);
        }

        var pathResult = ResolveFilePath(src, baseDirectory);
        if (pathResult.Status != ImageLoadStatus.Ok || pathResult.FullPath is null)
        {
            return CreateMetadata(src, pathResult.Status);
        }

        try
        {
            var length = new FileInfo(pathResult.FullPath).Length;
            if (length > maxBytes)
            {
                return CreateMetadata(src, ImageLoadStatus.Oversize);
            }
        }
        catch (IOException)
        {
            return CreateMetadata(src, ImageLoadStatus.Missing);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateMetadata(src, ImageLoadStatus.Missing);
        }

        var intrinsicSize = DecodeIntrinsicSize(pathResult.FullPath);
        if (intrinsicSize is null)
        {
            return CreateMetadata(src, ImageLoadStatus.DecodeFailed);
        }

        return MetadataOk(src, intrinsicSize.Value);
    }

    public static ImageResourceResult Load(string src, string? baseDirectory, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return Create(src, ImageLoadStatus.Missing);
        }

        var bytesResult = TryLoadBytes(src, baseDirectory);
        if (bytesResult.Status != ImageLoadStatus.Ok || bytesResult.Bytes is null)
        {
            return Create(src, bytesResult.Status);
        }

        var bytes = bytesResult.Bytes;
        if (bytes.LongLength > maxBytes)
        {
            return Create(src, ImageLoadStatus.Oversize);
        }

        var intrinsicSize = DecodeIntrinsicSize(bytes);
        if (intrinsicSize is null)
        {
            return Create(src, ImageLoadStatus.DecodeFailed);
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

        var pathResult = ResolveFilePath(src, baseDirectory);
        if (pathResult.Status != ImageLoadStatus.Ok || pathResult.FullPath is null)
        {
            return ImageBytesResult.Failed(pathResult.Status);
        }

        try
        {
            return ImageBytesResult.Ok(File.ReadAllBytes(pathResult.FullPath));
        }
        catch (IOException)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.Missing);
        }
        catch (UnauthorizedAccessException)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.Missing);
        }
    }

    private static ImagePathResult ResolveFilePath(string src, string? baseDirectory)
    {
        var resolvedBaseDirectory = ResolveBaseDirectory(baseDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(resolvedBaseDirectory, src));
        if (!IsWithinScope(fullPath, resolvedBaseDirectory))
        {
            return ImagePathResult.Failed(ImageLoadStatus.OutOfScope);
        }

        return File.Exists(fullPath)
            ? ImagePathResult.Ok(fullPath)
            : ImagePathResult.Failed(ImageLoadStatus.Missing);
    }

    private static ImageResourceMetadataResult CreateMetadataFromBytes(
        string src,
        ImageBytesResult bytesResult,
        long maxBytes)
    {
        if (bytesResult.Status != ImageLoadStatus.Ok || bytesResult.Bytes is null)
        {
            return CreateMetadata(src, bytesResult.Status);
        }

        var bytes = bytesResult.Bytes;
        if (bytes.LongLength > maxBytes)
        {
            return CreateMetadata(src, ImageLoadStatus.Oversize);
        }

        var intrinsicSize = DecodeIntrinsicSize(bytes);
        if (intrinsicSize is null)
        {
            return CreateMetadata(src, ImageLoadStatus.DecodeFailed);
        }

        return MetadataOk(src, intrinsicSize.Value);
    }

    private static SizePx? DecodeIntrinsicSize(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        return codec is null
            ? null
            : new SizePx(codec.Info.Width, codec.Info.Height);
    }

    private static SizePx? DecodeIntrinsicSize(string fullPath)
    {
        using var data = SKData.Create(fullPath);
        using var codec = data is null ? null : SKCodec.Create(data);
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
            return ImageBytesResult.Failed(ImageLoadStatus.InvalidDataUri);
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
            return ImageBytesResult.Failed(ImageLoadStatus.InvalidDataUri);
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

    private static ImageResourceResult Create(string src, ImageLoadStatus status) =>
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
            Status = ImageLoadStatus.Ok,
            Bytes = bytes,
            IntrinsicSizePx = intrinsicSizePx
        };

    private static ImageResourceMetadataResult CreateMetadata(string src, ImageLoadStatus status) =>
        new()
        {
            Src = src,
            Status = status,
            IntrinsicSizePx = new SizePx(0d, 0d)
        };

    private static ImageResourceMetadataResult MetadataOk(string src, SizePx intrinsicSizePx) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            IntrinsicSizePx = intrinsicSizePx
        };

    private readonly record struct ImagePathResult(ImageLoadStatus Status, string? FullPath)
    {
        public static ImagePathResult Ok(string fullPath) => new(ImageLoadStatus.Ok, fullPath);

        public static ImagePathResult Failed(ImageLoadStatus status) => new(status, null);
    }

    private readonly record struct ImageBytesResult(ImageLoadStatus Status, byte[]? Bytes)
    {
        public static ImageBytesResult Ok(byte[] bytes) => new(ImageLoadStatus.Ok, bytes);

        public static ImageBytesResult Failed(ImageLoadStatus status) => new(status, null);
    }
}
