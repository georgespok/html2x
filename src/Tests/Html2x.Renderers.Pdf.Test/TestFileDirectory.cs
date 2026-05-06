using Html2x.Text;

namespace Html2x.Renderers.Pdf.Test;

internal sealed class TestFileDirectory : IFileDirectory
{
    private readonly Dictionary<string, bool> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<FileEnumerationKey, IReadOnlyList<string>> _enumerations = new();
    private readonly Dictionary<string, bool> _files = new(StringComparer.OrdinalIgnoreCase);

    public List<string> FileExistsCalls { get; } = [];

    public List<string> DirectoryExistsCalls { get; } = [];

    public List<FileEnumerationKey> EnumerateFilesCalls { get; } = [];

    public List<string> GetExtensionCalls { get; } = [];

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
        _enumerations[new(directory, searchPattern, recursive)] = files;
        return this;
    }

    internal readonly record struct FileEnumerationKey(
        string Directory,
        string SearchPattern,
        bool Recursive);
}