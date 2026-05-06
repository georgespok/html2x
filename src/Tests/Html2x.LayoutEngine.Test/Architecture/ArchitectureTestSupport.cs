namespace Html2x.LayoutEngine.Test.Architecture;

internal static class ArchitectureTestSupport
{
    public const string PublicAccessibility = "public";

    public const string InternalAccessibility = "internal";

    public const string VoidTypeName = "void";

    public const string FacadeAssemblyName = "Html2x";

    public const string DiagnosticsAssemblyName = "Html2x.Diagnostics";

    public const string PdfRendererAssemblyName = "Html2x.Renderers.Pdf";

    public const string ResourcesAssemblyName = "Html2x.Resources";

    public const string SkiaSharpPackageName = "SkiaSharp";

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

    public static CSharpSourceFile SourceFileFor(Type type, params string[] containingFolders)
    {
        var pathSegments = new List<string> { "src", AssemblyName(type) };
        pathSegments.AddRange(containingFolders);
        pathSegments.Add(TypeName(type) + ".cs");

        return CSharpSourceFile.Load(pathSegments.ToArray());
    }

    public static string PathFromRoot(params string[] pathSegments) =>
        ArchitecturePaths.PathFromRoot(pathSegments);

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

    public static string ListTypeName<T>() => "List<" + TypeName<T>() + ">";

    public static string TaskTypeName<T>() => "Task<" + TypeName<T>() + ">";

    public static string FullTypeName<T>() =>
        typeof(T).FullName ?? throw new InvalidOperationException($"{typeof(T).Name} has no full name.");

    public static string FullTypeName(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"{type.Name} has no full name.");

    public static string NamespaceOf<T>() =>
        typeof(T).Namespace ?? throw new InvalidOperationException($"{typeof(T).Name} has no namespace.");

    public static string NamespaceOf(Type type) =>
        type.Namespace ?? throw new InvalidOperationException($"{type.Name} has no namespace.");

    public static string AssemblyName<T>() =>
        typeof(T).Assembly.GetName().Name ??
        throw new InvalidOperationException($"{typeof(T).Name} has no assembly name.");

    public static string AssemblyName(Type type) =>
        type.Assembly.GetName().Name ?? throw new InvalidOperationException($"{type.Name} has no assembly name.");

    public static string CurrentAssemblyName() =>
        typeof(ArchitectureTestSupport).Assembly.GetName().Name
        ?? throw new InvalidOperationException("The current assembly has no name.");

    public static string TestAssemblyNameFor<T>() => AssemblyName<T>() + ".Test";

    public static string ParserPackageName() => "Angle" + "Sharp";

    public static string ParserDomProviderName() => ParserPackageName() + "DomProvider";

    public static string StyleComputerTypeName() => "CssStyle" + "Computer";

    private static string ProjectFileName<T>() => AssemblyName<T>() + ".csproj";
}