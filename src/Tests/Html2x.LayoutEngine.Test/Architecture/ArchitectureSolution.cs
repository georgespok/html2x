using System.Text.RegularExpressions;

namespace Html2x.LayoutEngine.Test.Architecture;


internal sealed class ArchitectureSolution
{
    private readonly string _path;

    private ArchitectureSolution(string path)
    {
        _path = path;
    }

    public static ArchitectureSolution Load(params string[] pathSegments) =>
        new(ArchitecturePaths.PathFromRoot(pathSegments));

    public IReadOnlyList<string> ProjectNames()
    {
        return File.ReadLines(_path)
            .Select(static line => Regex.Match(line, "\"(?<path>[^\"]+\\.csproj)\""))
            .Where(static match => match.Success)
            .Select(static match => Path.GetFileNameWithoutExtension(match.Groups["path"].Value))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
