namespace Html2x.LayoutEngine.Test.Architecture;


internal static class ArchitecturePaths
{
    public static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Html2x.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    public static string PathFromRoot(params string[] pathSegments) =>
        Path.Combine([RepoRoot(), .. pathSegments]);

    public static bool IsBuildOutputPath(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length > 0 &&
            (segments[0].Equals("bin", StringComparison.OrdinalIgnoreCase) ||
             segments[0].Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
