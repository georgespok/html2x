using System.Text.RegularExpressions;

namespace Html2x.Architecture.Test.Support;

internal sealed class Solution
{
    private readonly string _path;

    private Solution(string path)
    {
        _path = path;
    }

    public static Solution Load(params string[] pathSegments) =>
        new(Paths.PathFromRoot(pathSegments));

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