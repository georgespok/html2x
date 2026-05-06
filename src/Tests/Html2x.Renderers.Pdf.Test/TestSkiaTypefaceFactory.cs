using Html2x.Text;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

internal sealed class TestSkiaTypefaceFactory : ISkiaTypefaceFactory
{
    private readonly Dictionary<FileFaceKey, SKTypeface?> _fileFaceTypefaces = new();
    private readonly Dictionary<string, SKTypeface?> _fileTypefaces = new(StringComparer.OrdinalIgnoreCase);

    public List<string> FromFileCalls { get; } = [];

    public List<FileFaceKey> FromFileWithFaceIndexCalls { get; } = [];

    public List<string> FromFamilyNameCalls { get; } = [];

    public SKTypeface? FromFile(string path)
    {
        FromFileCalls.Add(path);

        return _fileTypefaces.TryGetValue(path, out var typeface)
            ? typeface
            : throw new InvalidOperationException($"Unexpected FromFile call for '{path}'.");
    }

    public SKTypeface? FromFile(string path, int faceIndex)
    {
        var key = new FileFaceKey(path, faceIndex);
        FromFileWithFaceIndexCalls.Add(key);

        return _fileFaceTypefaces.TryGetValue(key, out var typeface)
            ? typeface
            : throw new InvalidOperationException($"Unexpected FromFile call for '{path}' face '{faceIndex}'.");
    }

    public SKTypeface? FromFamilyName(string family, SKFontStyle style)
    {
        FromFamilyNameCalls.Add(family);
        throw new InvalidOperationException($"Unexpected FromFamilyName call for '{family}'.");
    }

    public TestSkiaTypefaceFactory AddFileTypeface(string path, SKTypeface? typeface)
    {
        _fileTypefaces[path] = typeface;
        return this;
    }

    public TestSkiaTypefaceFactory AddFileTypeface(string path, int faceIndex, SKTypeface? typeface)
    {
        _fileFaceTypefaces[new(path, faceIndex)] = typeface;
        return this;
    }

    internal readonly record struct FileFaceKey(string Path, int FaceIndex);
}