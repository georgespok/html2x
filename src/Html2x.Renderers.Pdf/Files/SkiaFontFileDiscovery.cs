using Html2x.Abstractions.File;

namespace Html2x.Renderers.Pdf.Files;

internal static class SkiaFontFileDiscovery
{
    private static readonly string[] FontExtensions = [".ttf", ".otf", ".ttc"];

    public static IReadOnlyList<string> ListFontFiles(IFileDirectory fileDirectory, string directory)
    {
        ArgumentNullException.ThrowIfNull(fileDirectory);

        if (string.IsNullOrWhiteSpace(directory))
        {
            return [];
        }

        if (!fileDirectory.DirectoryExists(directory))
        {
            return [];
        }

        return fileDirectory.EnumerateFiles(directory, "*.*", recursive: true)
            .Where(path => FontExtensions.Contains(fileDirectory.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

