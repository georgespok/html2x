namespace Html2x.TestConsole;

internal static class ConsolePathResolver
{
    public static ConsolePaths Resolve(RenderSettings settings, string inputPath, string? selectedSamplePath)
    {
        var output = ResolveRequestedOutputPath(settings, inputPath);
        return new(
            Path.GetFullPath(inputPath),
            ResolveActualOutputPath(output.Path, output.WasExplicit),
            string.IsNullOrWhiteSpace(selectedSamplePath) ? null : Path.GetFullPath(selectedSamplePath));
    }

    private static (string Path, bool WasExplicit) ResolveRequestedOutputPath(RenderSettings settings,
        string? inputPath)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputOption))
        {
            return (settings.OutputOption, true);
        }

        if (!string.IsNullOrWhiteSpace(settings.Output))
        {
            return (settings.Output, true);
        }

        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "output" : fileName;
        return ($"{safeName}.pdf", false);
    }

    private static string ResolveActualOutputPath(string requestedPath, bool wasExplicit)
    {
        if (Path.IsPathRooted(requestedPath))
        {
            return requestedPath;
        }

        return wasExplicit
            ? Path.GetFullPath(requestedPath)
            : Path.Combine(Path.GetTempPath(), requestedPath);
    }
}