using System.Text;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Resources;
using SkiaSharp;

namespace Html2x.Resources;

internal static class ImageResourceLoader
{
    public static ImageResourceResult Load(string src, string? baseDirectory, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return Create(src, ImageLoadStatus.Missing);
        }

        var bytesResult = TryLoadBytes(src, baseDirectory, maxBytes);
        if (bytesResult.Status != ImageLoadStatus.Ok || bytesResult.Bytes is null)
        {
            return Create(src, bytesResult.Status);
        }

        var bytes = bytesResult.Bytes;
        if (bytes.LongLength > maxBytes)
        {
            return Create(src, ImageLoadStatus.Oversized);
        }

        var intrinsicSize = DecodeIntrinsicSize(bytes);
        if (intrinsicSize is null)
        {
            return Create(src, ImageLoadStatus.DecodeFailed);
        }

        return Ok(src, bytes, intrinsicSize.Value);
    }

    public static string ResolveBaseDirectory(string? baseDirectory) =>
        string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(baseDirectory);

    private static ImageBytesResult TryLoadBytes(string src, string? baseDirectory, long maxBytes)
    {
        if (IsDataUri(src))
        {
            return DecodeDataUri(src, maxBytes);
        }

        var pathResult = ResolveFilePath(src, baseDirectory);
        if (pathResult.Status != ImageLoadStatus.Ok || pathResult.FullPath is null)
        {
            return ImageBytesResult.Failed(pathResult.Status);
        }

        return LoadFileBytes(pathResult.FullPath, maxBytes);
    }

    private static ImageBytesResult LoadFileBytes(string fullPath, long maxBytes)
    {
        try
        {
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > maxBytes)
            {
                return ImageBytesResult.Failed(ImageLoadStatus.Oversized);
            }

            return ImageBytesResult.Ok(File.ReadAllBytes(fullPath));
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

    private static ImageBytesResult DecodeDataUri(string src, long maxBytes)
    {
        var commaIndex = src.IndexOf(',', StringComparison.Ordinal);
        if (commaIndex < 0 || commaIndex == src.Length - 1)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.InvalidDataUri);
        }

        var metadata = src.AsSpan(5, commaIndex - 5);
        var payload = src.AsSpan(commaIndex + 1);
        var isBase64 = metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);
        var estimatedBytes = isBase64
            ? EstimateBase64DecodedLength(payload)
            : EstimateTextDataUriByteCount(payload);
        if (estimatedBytes is null)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.InvalidDataUri);
        }

        if (estimatedBytes.Value > maxBytes)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.Oversized);
        }

        try
        {
            var bytes = isBase64
                ? Convert.FromBase64String(payload.ToString())
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload.ToString()));
            return ImageBytesResult.Ok(bytes);
        }
        catch (FormatException)
        {
            return ImageBytesResult.Failed(ImageLoadStatus.InvalidDataUri);
        }
    }

    /// <summary>
    ///     Estimates decoded byte length for a valid base64 payload without allocating the decoded buffer.
    /// </summary>
    private static long? EstimateBase64DecodedLength(ReadOnlySpan<char> payload)
    {
        long nonWhitespaceLength = 0;
        var paddingLength = 0;
        var hasPadding = false;

        foreach (var ch in payload)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (ch == '=')
            {
                hasPadding = true;
                paddingLength++;
                if (paddingLength > 2)
                {
                    return null;
                }

                nonWhitespaceLength++;
                continue;
            }

            if (hasPadding || !IsBase64ContentChar(ch))
            {
                return null;
            }

            nonWhitespaceLength++;
        }

        if (nonWhitespaceLength == 0)
        {
            return 0;
        }

        if (nonWhitespaceLength % 4 != 0)
        {
            return null;
        }

        return nonWhitespaceLength / 4 * 3 - paddingLength;
    }

    private static bool IsBase64ContentChar(char ch) =>
        ch is >= 'A' and <= 'Z' ||
        ch is >= 'a' and <= 'z' ||
        ch is >= '0' and <= '9' ||
        ch is '+' or '/';

    private static long? EstimateTextDataUriByteCount(ReadOnlySpan<char> payload)
    {
        long byteCount = 0;
        var index = 0;
        while (index < payload.Length)
        {
            if (payload[index] == '%')
            {
                if (index + 2 >= payload.Length ||
                    !IsHexDigit(payload[index + 1]) ||
                    !IsHexDigit(payload[index + 2]))
                {
                    return null;
                }

                byteCount++;
                index += 3;
                continue;
            }

            if (char.IsHighSurrogate(payload[index]) &&
                index + 1 < payload.Length &&
                char.IsLowSurrogate(payload[index + 1]))
            {
                byteCount += Encoding.UTF8.GetByteCount(payload.Slice(index, 2));
                index += 2;
                continue;
            }

            if (char.IsSurrogate(payload[index]))
            {
                return null;
            }

            byteCount += Encoding.UTF8.GetByteCount(payload.Slice(index, 1));
            index++;
        }

        return byteCount;
    }

    private static bool IsHexDigit(char ch) =>
        ch is >= '0' and <= '9' ||
        ch is >= 'A' and <= 'F' ||
        ch is >= 'a' and <= 'f';

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
            IntrinsicSizePx = new(0d, 0d)
        };

    private static ImageResourceResult Ok(string src, byte[] bytes, SizePx intrinsicSizePx) =>
        new()
        {
            Src = src,
            Status = ImageLoadStatus.Ok,
            Bytes = bytes,
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
