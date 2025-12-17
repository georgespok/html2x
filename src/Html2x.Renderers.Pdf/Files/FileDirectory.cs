using Html2x.Abstractions.File;

namespace Html2x.Renderers.Pdf.Files;

internal sealed class FileDirectory : IFileDirectory
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> EnumerateFiles(string directory, string searchPattern, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(directory, searchPattern, option);
    }

    public string GetExtension(string path) => Path.GetExtension(path);
}
