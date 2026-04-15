namespace Html2x.TestConsole;

internal static class ConsolePathResolver
{
    public static ConsolePaths Resolve(RenderSettings settings, string inputPath, string? selectedSamplePath)
    {
        var requestedOutputPath = ResolveRequestedOutputPath(settings, inputPath);
        return new ConsolePaths(
            Path.GetFullPath(inputPath),
            ResolveActualOutputPath(requestedOutputPath),
            string.IsNullOrWhiteSpace(selectedSamplePath) ? null : Path.GetFullPath(selectedSamplePath));
    }

    private static string ResolveRequestedOutputPath(RenderSettings settings, string? inputPath)
    {
        if (!string.IsNullOrWhiteSpace(settings.Output))
        {
            return settings.Output;
        }

        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "output" : fileName;
        return $"{safeName}.pdf";
    }

    private static string ResolveActualOutputPath(string requestedPath)
    {
        if (Path.IsPathRooted(requestedPath))
        {
            return requestedPath;
        }

        return Path.Combine(Path.GetTempPath(), requestedPath);
    }
}

internal sealed record ConsolePaths(
    string InputPath,
    string OutputPath,
    string? SelectedSamplePath);
