namespace Html2x.Architecture.Test.Support;

internal static class RepositoryLayout
{
    public const string SourceRoot = "src";
    public const string TestRoot = "Tests";
    public const string SolutionFileName = "Html2x.sln";
    public const string ProjectFileExtension = ".csproj";
    public const string CSharpFileExtension = ".cs";
    public const string CSharpFilePattern = "*" + CSharpFileExtension;
    public const string DllFilePattern = "*.dll";
    public const string BinDirectory = "bin";
    public const string ObjDirectory = "obj";
    public const string ReleaseConfiguration = "Release";
    public const string Net8TargetFramework = "net8.0";
    public const string GeneratedCSharpSuffix = ".g" + CSharpFileExtension;
    public const string AssemblyInfoCSharpSuffix = ".AssemblyInfo" + CSharpFileExtension;

    public static string ReleaseOutputPath() =>
        Path.Combine(BinDirectory, ReleaseConfiguration, Net8TargetFramework);
}
