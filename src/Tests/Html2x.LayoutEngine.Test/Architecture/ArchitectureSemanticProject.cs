using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;

internal sealed class ArchitectureSemanticProject
{
    private readonly CSharpCompilation _compilation;

    private ArchitectureSemanticProject(CSharpCompilation compilation)
    {
        _compilation = compilation;
    }

    public static ArchitectureSemanticProject Load(params string[] projectPathSegments)
    {
        var projectPath = ArchitecturePaths.PathFromRoot(projectPathSegments);
        var projectDirectory = Path.GetDirectoryName(projectPath)
                               ?? throw new InvalidOperationException($"Project path has no directory: {projectPath}");
        var syntaxTrees = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !ArchitecturePaths.IsBuildOutputPath(Path.GetRelativePath(projectDirectory, file)))
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file))
            .ToArray();
        var references = MetadataReferences();

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(projectPath),
            syntaxTrees,
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        return new(compilation);
    }

    public void ShouldNotReferenceNamespaces(params string[] namespaces)
    {
        var references = FindNamedSymbolReferences()
            .Where(reference => namespaces.Any(reference.IsInNamespace))
            .ToArray();

        references.ShouldBeEmpty("Forbidden namespace references were found:\n" +
                                 string.Join("\n", references.Select(static reference => reference.ToString())));
    }

    public void ShouldNotReferenceTypes(params string[] fullTypeNames)
    {
        var references = FindNamedSymbolReferences()
            .Where(reference => fullTypeNames.Contains(reference.FullTypeName, StringComparer.Ordinal))
            .ToArray();

        references.ShouldBeEmpty("Forbidden type references were found:\n" +
                                 string.Join("\n", references.Select(static reference => reference.ToString())));
    }

    public IReadOnlyList<string> PublicTypeNames()
    {
        return DeclaredTypeSymbols()
            .Where(static symbol => symbol.DeclaredAccessibility == Accessibility.Public)
            .Select(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> ExternallyVisibleTypeNames()
    {
        return DeclaredTypeSymbols()
            .Where(IsExternallyVisible)
            .Select(static symbol => symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<SymbolReference> FindNamedSymbolReferences()
    {
        var references = new List<SymbolReference>();

        foreach (var tree in _compilation.SyntaxTrees)
        {
            var model = _compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var node in root.DescendantNodes())
            {
                var symbol = SymbolForNode(model, node);
                if (symbol is null)
                {
                    continue;
                }

                var namedType = symbol as INamedTypeSymbol ??
                                symbol.ContainingType ??
                                (symbol as IMethodSymbol)?.ContainingType ??
                                (symbol as IPropertySymbol)?.ContainingType ??
                                (symbol as IFieldSymbol)?.ContainingType;
                if (namedType is null)
                {
                    continue;
                }

                references.Add(SymbolReference.Create(tree, node, namedType));
            }
        }

        return references
            .Distinct()
            .OrderBy(static reference => reference.RelativePath, StringComparer.Ordinal)
            .ThenBy(static reference => reference.LineNumber)
            .ThenBy(static reference => reference.FullTypeName, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<INamedTypeSymbol> DeclaredTypeSymbols()
    {
        return _compilation.SyntaxTrees
            .SelectMany(tree =>
            {
                var model = _compilation.GetSemanticModel(tree);
                return tree.GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Select(type => model.GetDeclaredSymbol(type))
                    .OfType<INamedTypeSymbol>();
            })
            .ToArray();
    }

    private static bool IsExternallyVisible(INamedTypeSymbol symbol)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        var containingType = symbol.ContainingType;
        while (containingType is not null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            containingType = containingType.ContainingType;
        }

        return true;
    }

    private static ISymbol? SymbolForNode(SemanticModel model, SyntaxNode node)
    {
        return node switch
        {
            ObjectCreationExpressionSyntax objectCreation => model.GetSymbolInfo(objectCreation).Symbol,
            IdentifierNameSyntax identifier => model.GetSymbolInfo(identifier).Symbol,
            QualifiedNameSyntax qualifiedName => model.GetSymbolInfo(qualifiedName).Symbol,
            MemberAccessExpressionSyntax memberAccess => model.GetSymbolInfo(memberAccess).Symbol,
            _ => null
        };
    }

    private static IReadOnlyList<MetadataReference> MetadataReferences()
    {
        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];
        foreach (var assembly in trustedAssemblies)
        {
            referencePaths.Add(assembly);
        }

        var repoRoot = ArchitecturePaths.RepoRoot();
        foreach (var assembly in Directory.GetFiles(repoRoot, "*.dll", SearchOption.AllDirectories)
                     .Where(path => path.Contains(Path.Combine("bin", "Release", "net8.0"),
                         StringComparison.OrdinalIgnoreCase)))
        {
            referencePaths.Add(assembly);
        }

        return referencePaths
            .Where(File.Exists)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private sealed record SymbolReference(
        string RelativePath,
        int LineNumber,
        string NamespaceName,
        string FullTypeName)
    {
        public static SymbolReference Create(SyntaxTree tree, SyntaxNode node, INamedTypeSymbol type)
        {
            var lineSpan = tree.GetLineSpan(node.Span);
            var fullPath = tree.FilePath;

            return new(
                Path.GetRelativePath(ArchitecturePaths.RepoRoot(), fullPath),
                lineSpan.StartLinePosition.Line + 1,
                type.ContainingNamespace.ToDisplayString(),
                type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        public bool IsInNamespace(string namespaceName) =>
            NamespaceName.Equals(namespaceName, StringComparison.Ordinal) ||
            NamespaceName.StartsWith(namespaceName + ".", StringComparison.Ordinal);

        public override string ToString() =>
            $"{RelativePath}:{LineNumber}: {FullTypeName}";
    }
}