using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.Architecture.Test.Support;

internal static class SourceLocator
{
    public static string SourcePathFor(Type type)
    {
        var projectDirectory = Paths.PathFromRoot(RepositoryLayout.SourceRoot, AssemblyName(type));
        var expectedFullName = SourceFullName(type);
        var matches = Directory.GetFiles(
                projectDirectory,
                RepositoryLayout.CSharpFilePattern,
                SearchOption.AllDirectories)
            .Where(file => !Paths.IsBuildOutputPath(Path.GetRelativePath(projectDirectory, file)))
            .Where(file => SourceDeclaresType(file, expectedFullName))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();

        matches.ShouldNotBeEmpty($"{projectDirectory} should contain source for {expectedFullName}.");
        matches.Length.ShouldBe(1, $"Source lookup for {expectedFullName} should resolve one file.");

        return matches[0];
    }

    private static bool SourceDeclaresType(string path, string expectedFullName)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
        var root = tree.GetCompilationUnitRoot();

        return root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Any(type => DeclaredFullName(type).Equals(expectedFullName, StringComparison.Ordinal));
    }

    private static string DeclaredFullName(BaseTypeDeclarationSyntax declaration)
    {
        var typeNames = new Stack<string>();
        SyntaxNode? current = declaration;
        while (current is not null)
        {
            if (current is BaseTypeDeclarationSyntax type)
            {
                typeNames.Push(type.Identifier.ValueText);
            }

            if (current is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                return namespaceDeclaration.Name + "." + string.Join(".", typeNames);
            }

            current = current.Parent;
        }

        return string.Join(".", typeNames);
    }

    private static string SourceFullName(Type type) =>
        (type.FullName ?? throw new InvalidOperationException($"{type.Name} has no full name."))
        .Replace("+", ".", StringComparison.Ordinal);

    private static string AssemblyName(Type type) =>
        type.Assembly.GetName().Name ?? throw new InvalidOperationException($"{type.Name} has no assembly name.");
}
