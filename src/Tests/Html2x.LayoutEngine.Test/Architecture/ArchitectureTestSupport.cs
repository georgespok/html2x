namespace Html2x.LayoutEngine.Test.Architecture;

internal static class ArchitectureTestSupport
{
    public static ArchitectureProject Project(params string[] pathSegments) =>
        ArchitectureProject.Load(pathSegments);

    public static ArchitectureProject ProjectFor<T>() =>
        ArchitectureProject.Load("src", AssemblyName<T>(), ProjectFileName<T>());

    public static ArchitectureSemanticProject SemanticProjectFor<T>() =>
        ArchitectureSemanticProject.Load("src", AssemblyName<T>(), ProjectFileName<T>());

    public static CSharpSourceSet SourceSetFor<T>() =>
        CSharpSourceSet.FromDirectory("src", AssemblyName<T>());

    public static CSharpSourceFile SourceFileFor<T>(params string[] containingFolders)
    {
        var pathSegments = new List<string> { "src", AssemblyName<T>() };
        pathSegments.AddRange(containingFolders);
        pathSegments.Add(TypeName<T>() + ".cs");

        return CSharpSourceFile.Load(pathSegments.ToArray());
    }

    public static string PathFromRoot(params string[] pathSegments) =>
        ArchitecturePaths.PathFromRoot(pathSegments);

    public static string TypeName<T>() => typeof(T).Name;

    public static string NullableTypeName<T>() => TypeName<T>() + "?";

    public static string FullTypeName<T>() =>
        typeof(T).FullName ?? throw new InvalidOperationException($"{typeof(T).Name} has no full name.");

    public static string NamespaceOf<T>() =>
        typeof(T).Namespace ?? throw new InvalidOperationException($"{typeof(T).Name} has no namespace.");

    public static string AssemblyName<T>() =>
        typeof(T).Assembly.GetName().Name ?? throw new InvalidOperationException($"{typeof(T).Name} has no assembly name.");

    public static string CurrentAssemblyName() =>
        typeof(ArchitectureTestSupport).Assembly.GetName().Name
        ?? throw new InvalidOperationException("The current assembly has no name.");

    public static string TestAssemblyNameFor<T>() => AssemblyName<T>() + ".Test";

    public static string ParserPackageName() => "Angle" + "Sharp";

    public static string ParserDomProviderName() => ParserPackageName() + "DomProvider";

    public static string StyleComputerTypeName() => "CssStyle" + "Computer";

    private static string ProjectFileName<T>() => AssemblyName<T>() + ".csproj";
}
