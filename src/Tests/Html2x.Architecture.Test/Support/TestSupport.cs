namespace Html2x.Architecture.Test.Support;

internal static class TestSupport
{
    public const string PublicAccessibility = "public";

    public const string InternalAccessibility = "internal";

    public const string VoidTypeName = "void";

    public static Project ProjectForAssembly<T>() =>
        ProjectFor<T>();

    public static Project ProjectFor<T>() =>
        Project.Load(RepositoryLayout.SourceRoot, AssemblyName<T>(), ProjectFileName<T>());

    public static Project ProjectFor(Type type) =>
        Project.Load(RepositoryLayout.SourceRoot, AssemblyName(type), ProjectFileName(type));

    public static Project TestProjectFor<T>()
    {
        var testAssemblyName = TestAssemblyNameFor<T>();
        return Project.Load(
            RepositoryLayout.SourceRoot,
            RepositoryLayout.TestRoot,
            testAssemblyName,
            ProjectFileName(testAssemblyName));
    }

    public static SemanticProject SemanticProjectForAssembly<T>() =>
        SemanticProjectFor<T>();

    public static SemanticProject SemanticProjectFor<T>() =>
        SemanticProject.Load(RepositoryLayout.SourceRoot, AssemblyName<T>(), ProjectFileName<T>());

    public static SemanticProject SemanticProjectFor(Type type) =>
        SemanticProject.Load(RepositoryLayout.SourceRoot, AssemblyName(type), ProjectFileName(type));

    public static WorkspaceProject WorkspaceProjectFor<T>() =>
        WorkspaceProject.Load(RepositoryLayout.SourceRoot, AssemblyName<T>(), ProjectFileName<T>());

    public static WorkspaceProject CurrentWorkspaceProject() =>
        WorkspaceProject.Load(
            RepositoryLayout.SourceRoot,
            RepositoryLayout.TestRoot,
            CurrentAssemblyName(),
            ProjectFileName(CurrentAssemblyName()));

    public static CSharpSourceSet SourceSetFor<T>() =>
        CSharpSourceSet.FromDirectory(RepositoryLayout.SourceRoot, AssemblyName<T>());

    public static CSharpSourceSet SourceSetFor(Type type) =>
        CSharpSourceSet.FromDirectory(RepositoryLayout.SourceRoot, AssemblyName(type));

    public static CSharpSourceSet SourceSetForTestAssemblyOf<T>() =>
        CSharpSourceSet.FromDirectory(RepositoryLayout.SourceRoot, RepositoryLayout.TestRoot, TestAssemblyNameFor<T>());

    public static CSharpSourceSet SourceSetForNamespaceOf<T>() =>
        SourceSetForNamespaceOf(typeof(T));

    public static CSharpSourceSet SourceSetForNamespaceOf(Type type)
    {
        var pathSegments = new List<string> { RepositoryLayout.SourceRoot, AssemblyName(type) };
        pathSegments.AddRange(NamespacePathSegments(type));
        return CSharpSourceSet.FromDirectory(pathSegments.ToArray());
    }

    public static CSharpSourceFile SourceFileFor<T>(params string[] containingFolders)
    {
        if (containingFolders.Length == 0)
        {
            return CSharpSourceFile.Load(SourceLocator.SourcePathFor(typeof(T)));
        }

        var pathSegments = new List<string> { RepositoryLayout.SourceRoot, AssemblyName<T>() };
        pathSegments.AddRange(containingFolders);
        pathSegments.Add(TypeName<T>() + RepositoryLayout.CSharpFileExtension);

        return CSharpSourceFile.Load(pathSegments.ToArray());
    }

    public static CSharpSourceFile SourceFileFor(Type type, params string[] containingFolders)
    {
        if (containingFolders.Length == 0)
        {
            return CSharpSourceFile.Load(SourceLocator.SourcePathFor(type));
        }

        var pathSegments = new List<string> { RepositoryLayout.SourceRoot, AssemblyName(type) };
        pathSegments.AddRange(containingFolders);
        pathSegments.Add(TypeName(type) + RepositoryLayout.CSharpFileExtension);

        return CSharpSourceFile.Load(pathSegments.ToArray());
    }

    public static string SourcePathFor<T>() => SourceLocator.SourcePathFor(typeof(T));

    public static string SourceDirectoryFor<T>() =>
        Paths.PathFromRoot(RepositoryLayout.SourceRoot, AssemblyName<T>());

    public static string SourceDirectoryFor(Type type) =>
        Paths.PathFromRoot(RepositoryLayout.SourceRoot, AssemblyName(type));

    public static string RelativeSourcePathFor<T>() => RelativeSourcePathFor(typeof(T));

    public static string RelativeSourcePathFor(Type type) =>
        Path.GetRelativePath(Paths.RepoRoot(), SourceLocator.SourcePathFor(type))
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

    public static string NamespacePrefix(string namespaceName, int segmentCount)
    {
        var segments = namespaceName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segmentCount <= 0 || segmentCount > segments.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

        return string.Join(".", segments.Take(segmentCount));
    }

    public static string PathFromRoot(params string[] pathSegments) =>
        Paths.PathFromRoot(pathSegments);

    public static string TypeName<T>() => typeof(T).Name;

    public static string TypeName(Type type) => type.Name;

    public static string CSharpTypeName<T>() => typeof(T) switch
    {
        { } type when type == typeof(bool) => "bool",
        { } type when type == typeof(int) => "int",
        { } type when type == typeof(long) => "long",
        { } type when type == typeof(string) => "string",
        _ => TypeName<T>()
    };

    public static string NullableTypeName<T>() => TypeName<T>() + "?";

    public static string NullableCSharpTypeName<T>() => CSharpTypeName<T>() + "?";

    public static string ReadOnlyListTypeName<T>() => "IReadOnlyList<" + TypeName<T>() + ">";

    public static string ReadOnlySetTypeName<T>() => "IReadOnlySet<" + CSharpTypeName<T>() + ">";

    public static string InterfaceListTypeName<T>() => "IList<" + TypeName<T>() + ">";

    public static string ListTypeName<T>() => "List<" + TypeName<T>() + ">";

    public static string NullableFuncTypeName<T>() => "Func<" + TypeName<T>() + ">?";

    public static string TaskTypeName<T>() => "Task<" + TypeName<T>() + ">";

    public static string FullTypeName<T>() =>
        typeof(T).FullName ?? throw new InvalidOperationException($"{typeof(T).Name} has no full name.");

    public static string FullTypeName(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"{type.Name} has no full name.");

    public static string NamespaceOf<T>() =>
        typeof(T).Namespace ?? throw new InvalidOperationException($"{typeof(T).Name} has no namespace.");

    public static string NamespaceOf(Type type) =>
        type.Namespace ?? throw new InvalidOperationException($"{type.Name} has no namespace.");

    public static string NamespaceSegmentOf<T>() => NamespaceSegmentOf(typeof(T));

    public static string NamespaceSegmentOf(Type type) =>
        NamespaceOf(type).Split('.').Last();

    public static string AssemblyName<T>() =>
        typeof(T).Assembly.GetName().Name ??
        throw new InvalidOperationException($"{typeof(T).Name} has no assembly name.");

    public static string AssemblyName(Type type) =>
        type.Assembly.GetName().Name ?? throw new InvalidOperationException($"{type.Name} has no assembly name.");

    public static string CurrentAssemblyName() =>
        typeof(TestSupport).Assembly.GetName().Name
        ?? throw new InvalidOperationException("The current assembly has no name.");

    public static string TestAssemblyNameFor<T>() => AssemblyName<T>() + ".Test";

    public static string ParserPackageName() => ExternalPackageIds.AngleSharp;

    public static string ParserDomProviderName() => ParserPackageName() + "DomProvider";

    public static string StyleComputerTypeName() => "CssStyle" + "Computer";

    private static string ProjectFileName<T>() => ProjectFileName(AssemblyName<T>());

    private static string ProjectFileName(Type type) => ProjectFileName(AssemblyName(type));

    private static string ProjectFileName(string assemblyName) => assemblyName + RepositoryLayout.ProjectFileExtension;

    private static IEnumerable<string> NamespacePathSegments(Type type)
    {
        var assemblySegments = AssemblyName(type).Split('.', StringSplitOptions.RemoveEmptyEntries);
        var namespaceSegments = NamespaceOf(type).Split('.', StringSplitOptions.RemoveEmptyEntries);
        return namespaceSegments.Skip(assemblySegments.Length);
    }
}
