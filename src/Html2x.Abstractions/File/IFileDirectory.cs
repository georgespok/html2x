using System.Collections.Generic;

namespace Html2x.Abstractions.File;

public interface IFileDirectory
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    /// <summary>
    /// Enumerates files under <paramref name="directory"/> matching <paramref name="searchPattern"/>.
    /// Implementations must return full paths.
    /// </summary>
    IEnumerable<string> EnumerateFiles(string directory, string searchPattern, bool recursive);

    string GetExtension(string path);
}

