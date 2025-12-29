using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Styles;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Drawing;

/// <summary>
/// Builds a metadata index of font files in a directory for consistent font selection.
/// </summary>
public static class FontDirectoryIndex
{
    private static readonly string[] FontExtensions = [".ttf", ".otf", ".ttc"];

    public static IReadOnlyList<FontFaceEntry> Build(IFileDirectory fileDirectory, ISkiaTypefaceFactory typefaceFactory, string directory)
    {
        ArgumentNullException.ThrowIfNull(fileDirectory);
        ArgumentNullException.ThrowIfNull(typefaceFactory);

        if (string.IsNullOrWhiteSpace(directory) || !fileDirectory.DirectoryExists(directory))
        {
            return [];
        }

        var files = ListFontFiles(fileDirectory, directory);
        if (files.Count == 0)
        {
            return [];
        }

        var faces = new List<FontFaceEntry>(capacity: files.Count);

        foreach (var file in files)
        {
            var ext = fileDirectory.GetExtension(file);
            if (string.Equals(ext, ".ttc", StringComparison.OrdinalIgnoreCase))
            {
                LoadCollectionFaces(typefaceFactory, file, faces);
                continue;
            }

            var tf = typefaceFactory.FromFile(file);
            if (tf is null)
            {
                continue;
            }

            faces.Add(ToEntry(file, faceIndex: 0, tf));

            if (!IsDefaultTypeface(tf))
            {
                tf.Dispose();
            }
        }

        return faces;
    }

    public static FontFaceEntry? FindBestMatch(IReadOnlyList<FontFaceEntry> faces, FontKey key)
    {
        if (faces.Count == 0)
        {
            return null;
        }

        var wantsItalic = key.Style is FontStyle.Italic or FontStyle.Oblique;
        var requestedWeight = (int)key.Weight;
        var familyCandidates = GetFamilyCandidates(key.Family).ToArray();

        FontFaceEntry? best = null;
        var bestSlantMatch = false;
        var bestWeightDistance = int.MaxValue;

        foreach (var family in familyCandidates)
        {
            for (var i = 0; i < faces.Count; i++)
            {
                var entry = faces[i];
                if (!string.Equals(entry.Family, family, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var slantMatch = entry.IsItalic == wantsItalic;
                var weightDistance = Math.Abs(entry.Weight - requestedWeight);

                if (best is null)
                {
                    best = entry;
                    bestSlantMatch = slantMatch;
                    bestWeightDistance = weightDistance;
                    continue;
                }

                if (bestSlantMatch != slantMatch)
                {
                    if (slantMatch)
                    {
                        best = entry;
                        bestSlantMatch = true;
                        bestWeightDistance = weightDistance;
                    }

                    continue;
                }

                if (weightDistance < bestWeightDistance)
                {
                    best = entry;
                    bestWeightDistance = weightDistance;
                    continue;
                }

                if (weightDistance == bestWeightDistance)
                {
                    var pathComparison = StringComparer.OrdinalIgnoreCase.Compare(entry.Path, best.Path);
                    if (pathComparison < 0 || (pathComparison == 0 && entry.FaceIndex < best.FaceIndex))
                    {
                        best = entry;
                        bestWeightDistance = weightDistance;
                    }
                }
            }
        }

        return best;
    }

    private static IReadOnlyList<string> ListFontFiles(IFileDirectory fileDirectory, string directory)
    {
        if (!fileDirectory.DirectoryExists(directory))
        {
            return [];
        }

        return fileDirectory.EnumerateFiles(directory, "*.*", recursive: true)
            .Where(path => FontExtensions.Contains(fileDirectory.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void LoadCollectionFaces(ISkiaTypefaceFactory typefaceFactory, string file, List<FontFaceEntry> faces)
    {
        for (var index = 0; ; index++)
        {
            var tf = typefaceFactory.FromFile(file, index);
            if (tf is null)
            {
                break;
            }

            faces.Add(ToEntry(file, index, tf));

            if (!IsDefaultTypeface(tf))
            {
                tf.Dispose();
            }
        }
    }

    private static FontFaceEntry ToEntry(string path, int faceIndex, SKTypeface typeface)
    {
        return new FontFaceEntry(
            path,
            faceIndex,
            typeface.FamilyName ?? string.Empty,
            typeface.FontWeight,
            typeface.IsItalic || typeface.FontSlant != SKFontStyleSlant.Upright);
    }

    private static IEnumerable<string> GetFamilyCandidates(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            yield return SKTypeface.Default.FamilyName;
            yield break;
        }

        yield return family;

        if (string.Equals(family, "Arial", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Liberation Sans";
            yield return "Helvetica";
        }

        yield return SKTypeface.Default.FamilyName;
    }

    private static bool IsDefaultTypeface(SKTypeface typeface)
    {
        return ReferenceEquals(typeface, SKTypeface.Default) || typeface.Handle == SKTypeface.Default.Handle;
    }
}

public sealed record FontFaceEntry(string Path, int FaceIndex, string Family, int Weight, bool IsItalic);
