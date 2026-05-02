using Html2x.Text;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test;

internal sealed class TestFileDirectory : IFileDirectory
{
    private readonly Dictionary<string, bool> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<FileEnumerationKey, IReadOnlyList<string>> _enumerations = new();

    public List<string> FileExistsCalls { get; } = [];

    public List<string> DirectoryExistsCalls { get; } = [];

    public List<FileEnumerationKey> EnumerateFilesCalls { get; } = [];

    public List<string> GetExtensionCalls { get; } = [];

    public TestFileDirectory AddFile(string path, bool exists = true)
    {
        _files[path] = exists;
        return this;
    }

    public TestFileDirectory AddDirectory(string path, bool exists = true)
    {
        _directories[path] = exists;
        return this;
    }

    public TestFileDirectory AddEnumeration(
        string directory,
        string searchPattern,
        bool recursive,
        IReadOnlyList<string> files)
    {
        _enumerations[new FileEnumerationKey(directory, searchPattern, recursive)] = files;
        return this;
    }

    public bool FileExists(string path)
    {
        FileExistsCalls.Add(path);

        return _files.TryGetValue(path, out var exists)
            ? exists
            : throw new InvalidOperationException($"Unexpected FileExists call for '{path}'.");
    }

    public bool DirectoryExists(string path)
    {
        DirectoryExistsCalls.Add(path);

        return _directories.TryGetValue(path, out var exists)
            ? exists
            : throw new InvalidOperationException($"Unexpected DirectoryExists call for '{path}'.");
    }

    public IEnumerable<string> EnumerateFiles(string directory, string searchPattern, bool recursive)
    {
        var key = new FileEnumerationKey(directory, searchPattern, recursive);
        EnumerateFilesCalls.Add(key);

        return _enumerations.TryGetValue(key, out var files)
            ? files
            : throw new InvalidOperationException(
                $"Unexpected EnumerateFiles call for '{directory}', '{searchPattern}', recursive '{recursive}'.");
    }

    public string GetExtension(string path)
    {
        GetExtensionCalls.Add(path);
        return Path.GetExtension(path);
    }

    internal readonly record struct FileEnumerationKey(
        string Directory,
        string SearchPattern,
        bool Recursive);
}

internal sealed class TestSkiaTypefaceFactory : ISkiaTypefaceFactory
{
    private readonly Dictionary<string, SKTypeface?> _fileTypefaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<FileFaceKey, SKTypeface?> _fileFaceTypefaces = new();

    public List<string> FromFileCalls { get; } = [];

    public List<FileFaceKey> FromFileWithFaceIndexCalls { get; } = [];

    public List<string> FromFamilyNameCalls { get; } = [];

    public TestSkiaTypefaceFactory AddFileTypeface(string path, SKTypeface? typeface)
    {
        _fileTypefaces[path] = typeface;
        return this;
    }

    public TestSkiaTypefaceFactory AddFileTypeface(string path, int faceIndex, SKTypeface? typeface)
    {
        _fileFaceTypefaces[new FileFaceKey(path, faceIndex)] = typeface;
        return this;
    }

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

    internal readonly record struct FileFaceKey(string Path, int FaceIndex);
}
