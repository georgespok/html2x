using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;


internal sealed class ArchitectureSolution
{
    private readonly string path;

    private ArchitectureSolution(string path)
    {
        this.path = path;
    }

    public static ArchitectureSolution Load(params string[] pathSegments) =>
        new(ArchitecturePaths.PathFromRoot(pathSegments));

    public IReadOnlyList<string> ProjectNames()
    {
        return File.ReadLines(path)
            .Select(static line => Regex.Match(line, "\"(?<path>[^\"]+\\.csproj)\""))
            .Where(static match => match.Success)
            .Select(static match => Path.GetFileNameWithoutExtension(match.Groups["path"].Value))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
