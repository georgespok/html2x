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
            AddFaceEntries(typefaceFactory, file, faces);
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
        var candidates = GetFamilyCandidates(faces, key.Family);
        if (candidates.Count == 0)
        {
            return SelectBestMatch(faces, wantsItalic, requestedWeight);
        }

        return SelectBestMatch(candidates, wantsItalic, requestedWeight);
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

    private static void AddFaceEntries(ISkiaTypefaceFactory typefaceFactory, string file, List<FontFaceEntry> faces)
    {
        if (string.Equals(Path.GetExtension(file), ".ttc", StringComparison.OrdinalIgnoreCase))
        {
            LoadCollectionFaces(typefaceFactory, file, faces);
            return;
        }

        AddFaceEntry(typefaceFactory, file, faceIndex: 0, faces);
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

            AddFaceEntry(file, index, tf, faces);
        }
    }

    private static FontFaceEntry ToEntry(string path, int faceIndex, SKTypeface typeface) =>
        new(
            path,
            faceIndex,
            typeface.FamilyName ?? string.Empty,
            typeface.FontWeight,
            typeface.IsItalic || typeface.FontSlant != SKFontStyleSlant.Upright);

    private static List<FontFaceEntry> GetFamilyCandidates(IReadOnlyList<FontFaceEntry> faces, string? family) =>
        string.IsNullOrWhiteSpace(family) 
            ? [] 
            : faces.Where(entry => string.Equals(entry.Family, family, StringComparison.OrdinalIgnoreCase)).ToList();

    private static bool IsDefaultTypeface(SKTypeface typeface) => 
        ReferenceEquals(typeface, SKTypeface.Default) || typeface.Handle == SKTypeface.Default.Handle;

    private static FontFaceEntry? SelectBestMatch(
        IReadOnlyList<FontFaceEntry> candidates,
        bool wantsItalic,
        int requestedWeight)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        // Selection order: slant match, weight distance, family, path, face index.
        var best = candidates[0];
        var bestScore = GetMatchScore(best, wantsItalic, requestedWeight);

        for (var i = 1; i < candidates.Count; i++)
        {
            var entry = candidates[i];
            var score = GetMatchScore(entry, wantsItalic, requestedWeight);
            if (CompareScores(score, bestScore) < 0)
            {
                best = entry;
                bestScore = score;
            }
        }

        return best;
    }

    private static MatchScore GetMatchScore(FontFaceEntry entry, bool wantsItalic, int requestedWeight) =>
        new(
            entry.IsItalic == wantsItalic ? 0 : 1,
            Math.Abs(entry.Weight - requestedWeight),
            entry.Family ?? string.Empty,
            entry.Path,
            entry.FaceIndex);

    private static int CompareScores(MatchScore left, MatchScore right)
    {
        var slantCompare = left.SlantMismatch.CompareTo(right.SlantMismatch);
        if (slantCompare != 0)
        {
            return slantCompare;
        }

        var weightCompare = left.WeightDistance.CompareTo(right.WeightDistance);
        if (weightCompare != 0)
        {
            return weightCompare;
        }

        var familyCompare = StringComparer.OrdinalIgnoreCase.Compare(left.Family, right.Family);
        if (familyCompare != 0)
        {
            return familyCompare;
        }

        var pathCompare = StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path);
        if (pathCompare != 0)
        {
            return pathCompare;
        }

        return left.FaceIndex.CompareTo(right.FaceIndex);
    }

    private static void AddFaceEntry(
        ISkiaTypefaceFactory typefaceFactory,
        string path,
        int faceIndex,
        List<FontFaceEntry> faces)
    {
        var typeface = faceIndex > 0
            ? typefaceFactory.FromFile(path, faceIndex)
            : typefaceFactory.FromFile(path);

        if (typeface is null)
        {
            return;
        }

        AddFaceEntry(path, faceIndex, typeface, faces);
    }

    private static void AddFaceEntry(string path, int faceIndex, SKTypeface typeface, List<FontFaceEntry> faces)
    {
        faces.Add(ToEntry(path, faceIndex, typeface));

        if (!IsDefaultTypeface(typeface))
        {
            typeface.Dispose();
        }
    }
}